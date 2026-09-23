#include "internal.h"
#include <limits.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

static _Thread_local hidinput_owner *callback_owner;

hidinput_owner *hidinput_allocate(size_t max_report, size_t capacity,
                                   hidinput_receiver receiver, hidinput_terminal_cb terminal,
                                   void *context) {
    if (!max_report || !capacity || max_report > SIZE_MAX / capacity ||
        max_report > LONG_MAX || capacity > SIZE_MAX / sizeof(size_t) || !receiver) return NULL;
    hidinput_owner *o = calloc(1, sizeof(*o));
    if (!o) return NULL;
    o->slots = malloc(max_report * capacity);
    o->lengths = calloc(capacity, sizeof(size_t));
    o->delivery = dispatch_queue_create("hidinput.delivery", DISPATCH_QUEUE_SERIAL);
    if (!o->slots || !o->lengths || !o->delivery) {
        free(o->slots); free(o->lengths);
        if (o->delivery) dispatch_release(o->delivery);
        free(o); return NULL;
    }
    if (pthread_mutex_init(&o->mutex, NULL) != 0) {
        free(o->slots); free(o->lengths); dispatch_release(o->delivery); free(o); return NULL;
    }
    if (pthread_cond_init(&o->cond, NULL) != 0) {
        pthread_mutex_destroy(&o->mutex);
        free(o->slots); free(o->lengths); dispatch_release(o->delivery); free(o); return NULL;
    }
    o->max_report = max_report; o->capacity = capacity;
    o->receiver = receiver; o->terminal = terminal; o->context = context;
    return o;
}

static void deliver(void *context) {
    hidinput_owner *o = context;
    pthread_mutex_lock(&o->mutex);
    while (o->count) {
        size_t index = o->head;
        size_t length = o->lengths[index];
        o->delivering = 1;
        pthread_mutex_unlock(&o->mutex);
        callback_owner = o;
        o->receiver(o->context, o->slots + index * o->max_report, length);
        callback_owner = NULL;
        pthread_mutex_lock(&o->mutex);
        o->head = (o->head + 1) % o->capacity;
        o->count--; o->delivering = 0;
        pthread_cond_broadcast(&o->cond);
    }
    if (o->terminal_reason && !o->terminal_sent) {
        int reason = o->terminal_reason;
        o->terminal_sent = 1;
        pthread_mutex_unlock(&o->mutex);
        if (o->terminal) {
            callback_owner = o;
            o->terminal(o->context, reason);
            callback_owner = NULL;
        }
        pthread_mutex_lock(&o->mutex);
    }
    o->scheduled = 0;
    pthread_cond_broadcast(&o->cond);
    pthread_mutex_unlock(&o->mutex);
}

static void terminate_locked(hidinput_owner *o, int reason) {
    if (o->terminal_reason || o->cancelling || !o->started) return;
    o->terminal_reason = reason;
    if (reason != 1) o->fault = 1;
    if (!o->scheduled) {
        o->scheduled = 1;
        dispatch_async_f(o->delivery, o, deliver);
    }
    pthread_cond_broadcast(&o->cond);
}
void hidinput_incoming(hidinput_owner *o, const uint8_t *data, size_t length) {
    pthread_mutex_lock(&o->mutex);
    if (!o->cancelling && !o->terminal_reason && o->started) {
        if (!data || length > o->max_report || o->count == o->capacity) {
            terminate_locked(o, data && length <= o->max_report ? 2 : 3);
            pthread_mutex_unlock(&o->mutex);
            hidinput_cancel(o);
            return;
        }
        size_t index = (o->head + o->count) % o->capacity;
        memcpy(o->slots + index * o->max_report, data, length);
        o->lengths[index] = length;
        o->count++;
        if (!o->scheduled) {
            o->scheduled = 1;
            dispatch_async_f(o->delivery, o, deliver);
        }
    }
    pthread_mutex_unlock(&o->mutex);
}

void hidinput_removed(hidinput_owner *o) {
    pthread_mutex_lock(&o->mutex);
    terminate_locked(o, 1);
    pthread_mutex_unlock(&o->mutex);
    hidinput_cancel(o);
}
void hidinput_report_fault(hidinput_owner *o) {
    pthread_mutex_lock(&o->mutex);
    terminate_locked(o, 3);
    pthread_mutex_unlock(&o->mutex);
    hidinput_cancel(o);
}
void hidinput_acked(hidinput_owner *o) {
    pthread_mutex_lock(&o->mutex);
    o->acked = 1;
    pthread_cond_broadcast(&o->cond);
    /* Last owner access: callers may destroy after observing this ack. */
    pthread_mutex_unlock(&o->mutex);
}
hidinput_result hidinput_start(hidinput_owner *o) {
    if (!o) return HIDINPUT_INVALID;
    pthread_mutex_lock(&o->mutex);
    if (o->started || o->cancelling) { pthread_mutex_unlock(&o->mutex); return HIDINPUT_INVALID; }
    o->started = 1;
    /* register_all is synchronous and may not call owner callbacks before activation;
       synthetic activation follows this same ordering. */
    o->backend->register_all(o);
    pthread_mutex_unlock(&o->mutex);
    return HIDINPUT_OK;
}
void hidinput_cancel(hidinput_owner *o) {
    if (!o) return;
    pthread_mutex_lock(&o->mutex);
    int first = o->started && !o->cancelling;
    if (first) o->cancelling = 1;
    pthread_mutex_unlock(&o->mutex);
    if (first) o->backend->cancel(o);
}
static int complete(hidinput_owner *o) {
    return o->started && o->acked && !o->scheduled && !o->delivering && !o->count &&
        (!o->terminal_reason || o->terminal_sent);
}
hidinput_result hidinput_wait_shutdown(hidinput_owner *o, uint32_t timeout_ms) {
    if (!o) return HIDINPUT_INVALID;
    if (callback_owner == o) return HIDINPUT_SELF_WAIT;
    pthread_mutex_lock(&o->mutex);
    if (!o->started) { pthread_mutex_unlock(&o->mutex); return HIDINPUT_INVALID; }
    struct timespec deadline;
    clock_gettime(CLOCK_REALTIME, &deadline);
    deadline.tv_sec += timeout_ms / 1000;
    deadline.tv_nsec += (long)(timeout_ms % 1000) * 1000000L;
    deadline.tv_sec += deadline.tv_nsec / 1000000000L;
    deadline.tv_nsec %= 1000000000L;
    while (!complete(o)) {
        int result = timeout_ms == UINT32_MAX ? pthread_cond_wait(&o->cond, &o->mutex) :
            pthread_cond_timedwait(&o->cond, &o->mutex, &deadline);
        if (result) { pthread_mutex_unlock(&o->mutex); return HIDINPUT_TIMEOUT; }
    }
    hidinput_result result = o->fault ? HIDINPUT_FAULT : HIDINPUT_OK;
    pthread_mutex_unlock(&o->mutex);
    return result;
}
hidinput_result hidinput_destroy(hidinput_owner *o) {
    if (!o || callback_owner == o) return HIDINPUT_BUSY;
    pthread_mutex_lock(&o->mutex);
    if (o->started && !complete(o)) { pthread_mutex_unlock(&o->mutex); return HIDINPUT_BUSY; }
    if (o->close_failed) { pthread_mutex_unlock(&o->mutex); return HIDINPUT_CLOSE_FAULT; }
    /* An unused owner has no backend callbacks. */
    pthread_mutex_unlock(&o->mutex);
    /* Potentially blocking native close runs on caller thread, never the event queue.
       External callers must serialize destroy against all other external calls. */
    if (o->backend->try_release(o) != HIDINPUT_OK) {
        o->close_failed = 1;
        return HIDINPUT_CLOSE_FAULT;
    }
    hidinput_discard(o);
    return HIDINPUT_OK;
}
void hidinput_discard(hidinput_owner *o) {
    dispatch_release(o->delivery);
    pthread_cond_destroy(&o->cond);
    pthread_mutex_destroy(&o->mutex);
    free(o->slots); free(o->lengths); free(o);
}
