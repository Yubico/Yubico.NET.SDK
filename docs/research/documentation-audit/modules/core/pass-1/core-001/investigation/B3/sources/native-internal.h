#ifndef HIDINPUT_INTERNAL_H
#define HIDINPUT_INTERNAL_H
#include "owner.h"
#include <dispatch/dispatch.h>
#include <IOKit/hid/IOHIDDevice.h>
#include <pthread.h>

typedef struct {
    /* register_all configures queue, callbacks and cancel handler, then activates.
       No fallible operations may occur after registration starts. */
    void (*register_all)(hidinput_owner *owner);
    void (*cancel)(hidinput_owner *owner);
    /* Called only after native ack and delivery drain (or before start).
       A failed close MUST retain all backend storage and never be retried. */
    hidinput_result (*try_release)(hidinput_owner *owner);
} hidinput_backend;

struct hidinput_owner {
    pthread_mutex_t mutex;
    pthread_cond_t cond;
    dispatch_queue_t delivery;
    uint8_t *slots;
    size_t *lengths;
    size_t max_report, capacity, head, count;
    int started, cancelling, acked, fault, scheduled, delivering, close_failed;
    int service_terminated; /* Successful IOKit service-termination callback only. */
    int terminal_reason, terminal_sent;
    hidinput_receiver receiver;
    hidinput_terminal_cb terminal;
    void *context;
    const hidinput_backend *backend;
    void *backend_data;
};
hidinput_owner *hidinput_allocate(size_t max_report, size_t capacity,
                                   hidinput_receiver receiver, hidinput_terminal_cb terminal,
                                   void *context);
void hidinput_discard(hidinput_owner *owner);
void hidinput_incoming(hidinput_owner *owner, const uint8_t *data, size_t length);
void hidinput_removed(hidinput_owner *owner);
void hidinput_acked(hidinput_owner *owner);
void hidinput_report_fault(hidinput_owner *owner);
/* Called by try_release only after the owner has completed cancellation/drain. */
int hidinput_close_proven(hidinput_owner *owner, IOReturn status);
/* The IOHID callback and macOS harness both enter through this validation seam. */
void hidinput_iohid_input(hidinput_owner *owner, IOReturn status, IOHIDReportType type,
                          uint32_t report_id, const uint8_t *report, CFIndex length);
void hidinput_iohid_removed(hidinput_owner *owner, IOReturn status);
#endif
