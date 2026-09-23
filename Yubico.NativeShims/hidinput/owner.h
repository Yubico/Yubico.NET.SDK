#ifndef HIDINPUT_OWNER_H
#define HIDINPUT_OWNER_H
#include <stddef.h>
#include <stdint.h>

/* Input-owner implementation shared by the macOS exports and standalone tests.
   All entry points take a live owner. The caller must serialize destroy against
   other external calls; BUSY leaves the owner alive. Receiver must not destroy
   or wait from inside its callback. Cancel before start is a no-op; an owner
   never started can be destroyed without a native cancellation acknowledgment. */
typedef struct hidinput_owner hidinput_owner;
typedef enum {
    HIDINPUT_OK = 0, HIDINPUT_BUSY = 1, HIDINPUT_TIMEOUT = 2,
    HIDINPUT_SELF_WAIT = 3, HIDINPUT_FAULT = 4, HIDINPUT_INVALID = 5,
    HIDINPUT_CLOSE_FAULT = 6
} hidinput_result;
/* Data is borrowed for this call only. Copy it to retain it. */
typedef void (*hidinput_receiver)(void *context, const uint8_t *data, size_t length);
/* Terminal reasons: removed=1, overflow=2, malformed/report failure=3. */
typedef void (*hidinput_terminal_cb)(void *context, int32_t reason);

hidinput_result hidinput_start(hidinput_owner *owner);
void hidinput_cancel(hidinput_owner *owner);
/* timeout_ms == UINT32_MAX waits indefinitely; a timeout never frees resources.
   FAULT here indicates a report/overflow fault, not a native close failure. */
hidinput_result hidinput_wait_shutdown(hidinput_owner *owner, uint32_t timeout_ms);
/* Synchronous, possibly blocking checked close on caller thread after native ack
   and accepted-delivery drain. CLOSE_FAULT retains owner and backend permanently:
   repeated destroy returns CLOSE_FAULT without retrying close. */
hidinput_result hidinput_destroy(hidinput_owner *owner);

/* Shipping macOS-only exports (other platforms have no input-owner symbols). */
hidinput_owner *Native_HidInputCreate(void *already_open_device, size_t max_report,
    size_t capacity, hidinput_receiver report_cb, hidinput_terminal_cb terminal_cb,
    void *context);
int Native_HidInputStart(hidinput_owner *owner);
void Native_HidInputCancel(hidinput_owner *owner);
int Native_HidInputWaitShutdown(hidinput_owner *owner, uint32_t timeout_ms);
int Native_HidInputDestroy(hidinput_owner *owner);

/* Caller supplies an already-open IOHIDDeviceRef exclusively for this owner,
   with no run-loop or dispatch queue previously assigned. The caller keeps its
   creation reference until successful destroy and then releases it; the owner
   separately retains its reference until successful destroy. No open is performed;
   destroy checks IOHIDDeviceClose after cancellation acknowledgment. A
   kIOReturnNoDevice close after a native removal terminal and completed drain
    permits release; BadArgument additionally requires confirmed service
    termination, a removed terminal and completed drain. Other failures report
    HIDINPUT_CLOSE_FAULT.
   If never started, the caller remains responsible for closing the device. */
hidinput_owner *hidinput_iohid_create(void *device, size_t max_report, size_t capacity,
                                      hidinput_receiver receiver, hidinput_terminal_cb terminal,
                                      void *context);

/* Synthetic-only entry points in the nonshipping harness. */
hidinput_owner *hidinput_test_create(size_t max_report, size_t capacity,
                                     hidinput_receiver receiver, void *context);
hidinput_owner *hidinput_test_create_with_terminal(size_t max_report, size_t capacity,
    hidinput_receiver receiver, hidinput_terminal_cb terminal, void *context);
void hidinput_test_inject(hidinput_owner *owner, const uint8_t *data, size_t length);
void hidinput_test_ack(hidinput_owner *owner);
void hidinput_test_remove(hidinput_owner *owner);
int hidinput_test_activated_with_registration(hidinput_owner *owner);
void hidinput_test_fail_close(hidinput_owner *owner);
void hidinput_test_set_close_status(hidinput_owner *owner, int status);
int hidinput_test_close_attempts(hidinput_owner *owner);
#endif
