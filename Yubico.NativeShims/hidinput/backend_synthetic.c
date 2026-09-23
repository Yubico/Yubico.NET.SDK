#include "internal.h"
#include <stdlib.h>

typedef struct { int registered, activated, fail_close, close_attempts; } synthetic;
static void register_all(hidinput_owner *o) {
    synthetic *s = o->backend_data;
    /* Called under owner mutex: registration precedes activation. */
    s->registered = 1;
    s->activated = s->registered;
}
static void cancel_backend(hidinput_owner *o) { (void)o; }
static hidinput_result try_release(hidinput_owner *o) {
    synthetic *s = o->backend_data;
    if (o->started) {
        s->close_attempts++;
        if (s->fail_close) return HIDINPUT_CLOSE_FAULT;
    }
    free(s);
    return HIDINPUT_OK;
}
static const hidinput_backend backend = {register_all, cancel_backend, try_release};
hidinput_owner *hidinput_test_create(size_t max_report, size_t capacity,
                                     hidinput_receiver receiver, void *context) {
    hidinput_owner *o = hidinput_allocate(max_report, capacity, receiver, NULL, context);
    if (!o) return NULL;
    o->backend_data = calloc(1, sizeof(synthetic));
    if (!o->backend_data) { hidinput_discard(o); return NULL; }
    o->backend = &backend;
    return o;
}
hidinput_owner *hidinput_test_create_with_terminal(size_t max_report, size_t capacity,
    hidinput_receiver receiver, hidinput_terminal_cb terminal, void *context) {
    hidinput_owner *o = hidinput_test_create(max_report, capacity, receiver, context);
    if (o) o->terminal = terminal;
    return o;
}
void hidinput_test_inject(hidinput_owner *o, const uint8_t *data, size_t length) {
    /* The owner lock inside incoming is the sole authority on accepting reports.
       No unsynchronized synthetic cancellation flags or separate preflight check. */
    if (o) hidinput_incoming(o, data, length);
}
void hidinput_test_ack(hidinput_owner *o) {
    if (!o) return;
    pthread_mutex_lock(&o->mutex);
    int cancelling = o->cancelling;
    pthread_mutex_unlock(&o->mutex);
    if (cancelling) hidinput_acked(o);
}
void hidinput_test_remove(hidinput_owner *o) { if (o) hidinput_removed(o); }
int hidinput_test_activated_with_registration(hidinput_owner *o) {
    pthread_mutex_lock(&o->mutex);
    synthetic *s = o->backend_data;
    int result = s->activated && s->registered;
    pthread_mutex_unlock(&o->mutex);
    return result;
}
void hidinput_test_fail_close(hidinput_owner *o) {
    pthread_mutex_lock(&o->mutex);
    ((synthetic *)o->backend_data)->fail_close = 1;
    pthread_mutex_unlock(&o->mutex);
}
int hidinput_test_close_attempts(hidinput_owner *o) {
    pthread_mutex_lock(&o->mutex);
    int attempts = ((synthetic *)o->backend_data)->close_attempts;
    pthread_mutex_unlock(&o->mutex);
    return attempts;
}
