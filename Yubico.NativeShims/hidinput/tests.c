#include "internal.h"
#include <stdio.h>
#include <pthread.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#undef assert
#define assert(expr) do { if (!(expr)) { fprintf(stderr, "check failed: %s:%d: %s\n", __FILE__, __LINE__, #expr); abort(); } } while (0)

typedef struct {
    pthread_mutex_t mutex;
    pthread_cond_t cond;
    int entered, release, count;
    int terminal_count, terminal_reason, terminal_after_reports;
    uint8_t byte;
    hidinput_owner *owner;
    hidinput_result self_wait;
} capture;
static void terminal(void *ctx, int32_t reason) {
    capture *c = ctx;
    pthread_mutex_lock(&c->mutex);
    c->terminal_count++;
    c->terminal_reason = reason;
    c->terminal_after_reports = c->count;
    c->self_wait = hidinput_wait_shutdown(c->owner, 0);
    pthread_cond_broadcast(&c->cond);
    pthread_mutex_unlock(&c->mutex);
}

static void receive(void *ctx, const uint8_t *data, size_t length) {
    capture *c = ctx;
    pthread_mutex_lock(&c->mutex);
    c->byte = length ? data[0] : 0;
    c->count++;
    c->self_wait = hidinput_wait_shutdown(c->owner, 0);
    c->entered = 1;
    pthread_cond_broadcast(&c->cond);
    while (!c->release) pthread_cond_wait(&c->cond, &c->mutex);
    pthread_mutex_unlock(&c->mutex);
}

static void init(capture *c) {
    memset(c, 0, sizeof(*c));
    assert(pthread_mutex_init(&c->mutex, NULL) == 0);
    assert(pthread_cond_init(&c->cond, NULL) == 0);
}
static void unblock(capture *c) {
    pthread_mutex_lock(&c->mutex);
    c->release = 1;
    pthread_cond_broadcast(&c->cond);
    pthread_mutex_unlock(&c->mutex);
}
static void finish(capture *c) {
    pthread_cond_destroy(&c->cond);
    pthread_mutex_destroy(&c->mutex);
}

static void lifecycle(void) {
    capture c; init(&c);
    hidinput_owner *o = hidinput_test_create(8, 2, receive, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    assert(hidinput_test_activated_with_registration(o));
    assert(hidinput_start(o) == HIDINPUT_INVALID);
    uint8_t reusable[8] = {42};
    hidinput_test_inject(o, reusable, sizeof(reusable));
    memset(reusable, 99, sizeof(reusable));
    pthread_mutex_lock(&c.mutex);
    while (!c.entered) pthread_cond_wait(&c.cond, &c.mutex);
    assert(c.byte == 42);
    assert(c.self_wait == HIDINPUT_SELF_WAIT);
    pthread_mutex_unlock(&c.mutex);
    hidinput_cancel(o); hidinput_cancel(o);
    assert(hidinput_destroy(o) == HIDINPUT_BUSY);
    assert(hidinput_wait_shutdown(o, 0) == HIDINPUT_TIMEOUT);
    hidinput_test_ack(o);
    assert(hidinput_destroy(o) == HIDINPUT_BUSY);
    unblock(&c);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    hidinput_test_inject(o, reusable, sizeof(reusable));
    assert(c.count == 1);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void overflow(void) {
    capture c; init(&c);
    hidinput_owner *o = hidinput_test_create(1, 2, receive, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    uint8_t b = 7;
    hidinput_test_inject(o, &b, 1);
    pthread_mutex_lock(&c.mutex);
    while (!c.entered) pthread_cond_wait(&c.cond, &c.mutex);
    pthread_mutex_unlock(&c.mutex);
    hidinput_test_inject(o, &b, 1);
    hidinput_test_inject(o, &b, 1);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 0) == HIDINPUT_TIMEOUT);
    unblock(&c);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_FAULT);
    assert(c.count == 2);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void removal(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create(1, 1, receive, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_test_remove(o);
    assert(hidinput_destroy(o) == HIDINPUT_BUSY);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void invalid_creation(void) {
    capture c; init(&c);
    assert(!hidinput_test_create(0, 1, receive, &c));
    assert(!hidinput_test_create(1, 0, receive, &c));
    assert(!hidinput_test_create(SIZE_MAX, 2, receive, &c));
    hidinput_owner *o = hidinput_test_create(1, 1, receive, &c);
    assert(o);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void oversize(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create(1, 1, receive, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    uint8_t b[2] = {0};
    hidinput_test_inject(o, b, 2);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_FAULT);
    assert(c.count == 0);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void separate_owners(void) {
    capture a, b; init(&a); init(&b);
    a.release = b.release = 1;
    hidinput_owner *first = hidinput_test_create(1, 1, receive, &a);
    hidinput_owner *next = hidinput_test_create(1, 1, receive, &b);
    assert(first && next); a.owner = first; b.owner = next;
    assert(hidinput_start(first) == HIDINPUT_OK);
    hidinput_cancel(first);
    hidinput_test_ack(first);
    assert(hidinput_wait_shutdown(first, 2000) == HIDINPUT_OK);
    assert(hidinput_start(next) == HIDINPUT_OK);
    uint8_t byte = 31;
    hidinput_test_inject(first, &byte, 1);
    hidinput_test_inject(next, &byte, 1);
    hidinput_cancel(next);
    hidinput_test_ack(next);
    assert(hidinput_wait_shutdown(next, 2000) == HIDINPUT_OK);
    assert(a.count == 0 && b.count == 1 && b.byte == 31);
    assert(hidinput_destroy(first) == HIDINPUT_OK);
    assert(hidinput_destroy(next) == HIDINPUT_OK);
    finish(&a); finish(&b);
}
static void close_failure_retains(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create(1, 1, receive, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_test_fail_close(o);
    hidinput_cancel(o);
    assert(hidinput_destroy(o) == HIDINPUT_BUSY);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
    assert(hidinput_test_close_attempts(o) == 1);
    assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
    assert(hidinput_test_close_attempts(o) == 1);
    assert(hidinput_wait_shutdown(o, 0) == HIDINPUT_OK);
    /* Deliberately retained: failed native close cannot be safely released. */
    finish(&c);
}
static void *inject_concurrently(void *context) {
    hidinput_owner *o = context;
    uint8_t byte = 9;
    for (int i = 0; i < 1000; i++) hidinput_test_inject(o, &byte, 1);
    return NULL;
}
static void concurrent_cancel(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create(1, 2, receive, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    pthread_t worker;
    assert(pthread_create(&worker, NULL, inject_concurrently, o) == 0);
    hidinput_cancel(o);
    assert(pthread_join(worker, NULL) == 0);
    hidinput_test_ack(o);
    hidinput_result result = hidinput_wait_shutdown(o, 2000);
    assert(result == HIDINPUT_OK || result == HIDINPUT_FAULT);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void terminal_removal(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 2, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    uint8_t b = 3;
    hidinput_test_inject(o, &b, 1);
    hidinput_test_remove(o);
    hidinput_test_remove(o);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(c.count == 1 && c.terminal_count == 1 && c.terminal_reason == 1);
    assert(c.terminal_after_reports == 1 && c.self_wait == HIDINPUT_SELF_WAIT);
    hidinput_test_inject(o, &b, 1);
    assert(c.count == 1 && c.terminal_count == 1);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void terminal_overflow(void) {
    capture c; init(&c);
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 2, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    uint8_t b = 7;
    hidinput_test_inject(o, &b, 1);
    pthread_mutex_lock(&c.mutex);
    while (!c.entered) pthread_cond_wait(&c.cond, &c.mutex);
    pthread_mutex_unlock(&c.mutex);
    hidinput_test_inject(o, &b, 1);
    hidinput_test_inject(o, &b, 1);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 0) == HIDINPUT_TIMEOUT);
    unblock(&c);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_FAULT);
    assert(c.count == 2 && c.terminal_count == 1 && c.terminal_reason == 2);
    assert(c.terminal_after_reports == 2 && c.self_wait == HIDINPUT_SELF_WAIT);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void iohid_metadata(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create_with_terminal(2, 2, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    uint8_t b = 7;
    hidinput_iohid_input(o, kIOReturnSuccess, kIOHIDReportTypeOutput, 0, &b, 1);
    hidinput_iohid_input(o, kIOReturnSuccess, kIOHIDReportTypeInput, 1, &b, 1);
    hidinput_iohid_input(o, kIOReturnSuccess, kIOHIDReportTypeInput, 0, &b, 1);
    hidinput_cancel(o); hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(c.count == 1 && c.terminal_count == 0 && c.byte == 7);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);

    for (int failure = 0; failure < 3; failure++) {
        init(&c); c.release = 1;
        o = hidinput_test_create_with_terminal(2, 2, receive, terminal, &c);
        assert(o); c.owner = o;
        assert(hidinput_start(o) == HIDINPUT_OK);
        hidinput_iohid_input(o, failure == 0 ? kIOReturnError : kIOReturnSuccess,
            kIOHIDReportTypeInput, 0, &b, failure == 1 ? -1 : failure == 2 ? 0 : 1);
        hidinput_test_ack(o);
        assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_FAULT);
        assert(c.count == 0 && c.terminal_count == 1 && c.terminal_reason == 3);
        assert(hidinput_destroy(o) == HIDINPUT_OK);
        finish(&c);
    }
}
static void iohid_no_device_input(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create_with_terminal(2, 2, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_iohid_input(o, kIOReturnNoDevice, kIOHIDReportTypeInput, 0, NULL, 0);
    hidinput_test_remove(o);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(c.count == 0 && c.terminal_count == 1 && c.terminal_reason == 1);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void close_status_after_removal(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 1, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_test_remove(o);
    hidinput_test_set_close_status(o, kIOReturnNoDevice);
    assert(hidinput_destroy(o) == HIDINPUT_BUSY);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(c.terminal_count == 1 && c.terminal_reason == 1);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void close_no_device_without_removal_retains(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create(1, 1, receive, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_test_set_close_status(o, kIOReturnNoDevice);
    hidinput_cancel(o); hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
    assert(hidinput_test_close_attempts(o) == 1);
    assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
    finish(&c);
}
static void callback_removal_and_unknown_close(void) {
    for (int unknown = 0; unknown < 2; unknown++) {
        capture c; init(&c); c.release = 1;
        hidinput_owner *o = hidinput_test_create_with_terminal(1, 1, receive, terminal, &c);
        assert(o); c.owner = o;
        assert(hidinput_start(o) == HIDINPUT_OK);
        hidinput_iohid_removed(o, kIOReturnNoDevice);
        hidinput_iohid_removed(o, kIOReturnNoDevice);
        hidinput_test_set_close_status(o, unknown ? kIOReturnError : kIOReturnNoDevice);
        hidinput_test_ack(o);
        assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
        assert(c.terminal_count == 1 && c.terminal_reason == 1);
        assert(hidinput_destroy(o) == (unknown ? HIDINPUT_CLOSE_FAULT : HIDINPUT_OK));
        if (unknown) assert(hidinput_test_close_attempts(o) == 1);
        finish(&c);
    }
}
static void unknown_removal_callback_is_fault(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 1, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_iohid_removed(o, kIOReturnError);
    hidinput_test_set_close_status(o, kIOReturnNoDevice);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_FAULT);
    assert(c.terminal_count == 1 && c.terminal_reason == 3);
    assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
    assert(hidinput_test_close_attempts(o) == 1);
    finish(&c);
}
static void removal_close_waits_for_delivery_drain(void) {
    capture c; init(&c);
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 2, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    uint8_t b = 7;
    hidinput_test_inject(o, &b, 1);
    pthread_mutex_lock(&c.mutex);
    while (!c.entered) pthread_cond_wait(&c.cond, &c.mutex);
    pthread_mutex_unlock(&c.mutex);
    hidinput_test_remove(o);
    hidinput_test_set_close_status(o, kIOReturnNoDevice);
    hidinput_test_ack(o);
    assert(hidinput_destroy(o) == HIDINPUT_BUSY);
    assert(hidinput_test_close_attempts(o) == 0);
    unblock(&c);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(c.count == 1 && c.terminal_count == 1 && c.terminal_after_reports == 1);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void bad_argument_after_service_termination(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 1, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_iohid_removed(o, kIOReturnSuccess);
    hidinput_test_set_close_status(o, kIOReturnBadArgument);
    assert(hidinput_destroy(o) == HIDINPUT_BUSY);
    assert(hidinput_test_close_attempts(o) == 0);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(c.terminal_count == 1 && c.terminal_reason == 1);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void bad_argument_without_service_termination_retains(void) {
    for (int path = 0; path < 3; path++) {
        capture c; init(&c); c.release = 1;
        hidinput_owner *o = hidinput_test_create_with_terminal(1, 1, receive, terminal, &c);
        assert(o); c.owner = o;
        assert(hidinput_start(o) == HIDINPUT_OK);
        if (path == 0) hidinput_iohid_input(o, kIOReturnNoDevice, kIOHIDReportTypeInput, 0, NULL, 0);
        if (path == 1) hidinput_iohid_removed(o, kIOReturnNoDevice);
        if (path == 2) hidinput_iohid_removed(o, kIOReturnError);
        hidinput_test_set_close_status(o, kIOReturnBadArgument);
        hidinput_test_ack(o);
        assert(hidinput_wait_shutdown(o, 2000) == (path == 2 ? HIDINPUT_FAULT : HIDINPUT_OK));
        assert(c.terminal_count == 1 && c.terminal_reason == (path == 2 ? 3 : 1));
        assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
        assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
        assert(hidinput_test_close_attempts(o) == 1);
        finish(&c);
    }
}
static void bad_argument_after_fault_then_service_termination_retains(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 1, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_iohid_input(o, kIOReturnError, kIOHIDReportTypeInput, 0, NULL, 0);
    hidinput_iohid_removed(o, kIOReturnSuccess);
    hidinput_test_set_close_status(o, kIOReturnBadArgument);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_FAULT);
    assert(c.terminal_count == 1 && c.terminal_reason == 3);
    assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
    assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
    assert(hidinput_test_close_attempts(o) == 1);
    finish(&c);
}
static void bad_argument_service_termination_waits_for_delivery(void) {
    capture c; init(&c);
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 2, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    uint8_t b = 7;
    hidinput_test_inject(o, &b, 1);
    pthread_mutex_lock(&c.mutex);
    while (!c.entered) pthread_cond_wait(&c.cond, &c.mutex);
    pthread_mutex_unlock(&c.mutex);
    hidinput_iohid_removed(o, kIOReturnSuccess);
    hidinput_test_set_close_status(o, kIOReturnBadArgument);
    hidinput_test_ack(o);
    assert(hidinput_destroy(o) == HIDINPUT_BUSY);
    assert(hidinput_test_close_attempts(o) == 0);
    unblock(&c);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(c.count == 1 && c.terminal_count == 1 && c.terminal_after_reports == 1);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
static void unrelated_close_after_service_termination_retains(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 1, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_iohid_removed(o, kIOReturnSuccess);
    hidinput_test_set_close_status(o, kIOReturnNotOpen);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
    assert(hidinput_destroy(o) == HIDINPUT_CLOSE_FAULT);
    assert(hidinput_test_close_attempts(o) == 1);
    finish(&c);
}
static void input_no_device_then_service_termination(void) {
    capture c; init(&c); c.release = 1;
    hidinput_owner *o = hidinput_test_create_with_terminal(1, 1, receive, terminal, &c);
    assert(o); c.owner = o;
    assert(hidinput_start(o) == HIDINPUT_OK);
    hidinput_iohid_input(o, kIOReturnNoDevice, kIOHIDReportTypeInput, 0, NULL, 0);
    hidinput_iohid_removed(o, kIOReturnSuccess);
    hidinput_test_set_close_status(o, kIOReturnBadArgument);
    hidinput_test_ack(o);
    assert(hidinput_wait_shutdown(o, 2000) == HIDINPUT_OK);
    assert(c.terminal_count == 1 && c.terminal_reason == 1);
    assert(hidinput_destroy(o) == HIDINPUT_OK);
    finish(&c);
}
int main(int argc, char **argv) {
    assert(argc == 2);
    int test = atoi(argv[1]);
    switch (test) {
        case 1: lifecycle(); break;
        case 2: overflow(); break;
        case 3: removal(); break;
        case 4: invalid_creation(); break;
        case 5: oversize(); break;
        case 6: separate_owners(); break;
        case 7: close_failure_retains(); break;
        case 8: concurrent_cancel(); break;
        case 9: terminal_removal(); break;
        case 10: terminal_overflow(); break;
        case 11: iohid_metadata(); break;
        case 12: iohid_no_device_input(); break;
        case 13: close_status_after_removal(); break;
        case 14: close_no_device_without_removal_retains(); break;
        case 15: callback_removal_and_unknown_close(); break;
        case 16: unknown_removal_callback_is_fault(); break;
        case 17: removal_close_waits_for_delivery_drain(); break;
        case 18: bad_argument_after_service_termination(); break;
        case 19: bad_argument_without_service_termination_retains(); break;
        case 20: bad_argument_after_fault_then_service_termination_retains(); break;
        case 21: bad_argument_service_termination_waits_for_delivery(); break;
        case 22: unrelated_close_after_service_termination_retains(); break;
        case 23: input_no_device_then_service_termination(); break;
        default: assert(0);
    }
}
