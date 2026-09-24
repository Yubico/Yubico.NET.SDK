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

/* Shipping macOS-only ABI for the managed input source (no other-platform exports).
   This is one persistent, already-open device subscription, NOT one operation per
   report or a new submission for each Start. The five exports below are the
   managed entry points; result integers use hidinput_result values above.
   Create returns NULL for invalid arguments/allocation failure (including NULL
   device or callbacks, zero/overflowing sizes); it never calls either callback.
   Start returns OK only after queue, report/removal and cancel-handler
   registration followed by activation. INVALID (NULL owner or repeated Start,
   including after a started owner is cancelled) creates no new registration.
   Activation can cause callbacks on another queue before Start returns;
   callbacks are not inline Start calls.
   A successful Start does not promise any report: zero reports is valid, and
   multiple input reports may arrive during the same activation. Each accepted
   report is delivered once in order, with data borrowed only during the callback.
   The required terminal callback runs at most once per owner, after accepted
   reports drain: removed=1, overflow=2, malformed/report failure=3. A clean
   explicit Cancel does not promise a terminal callback. Cancel before Start is
   a no-op; after Start it requests cancellation, not completion. WaitShutdown
   waits for native cancellation acknowledgment AND accepted callback drain;
   UINT32_MAX means indefinite wait. OK means quiescent, FAULT means quiescent
   after report/overflow fault, TIMEOUT leaves resources live; neither OK nor
   FAULT proves close succeeded. Destroy is the checked, potentially blocking
   close: BUSY retains the owner, OK frees it, CLOSE_FAULT retains owner/backend
   permanently and does not retry close. Never wait/destroy inside a callback;
   serialize Destroy with all other external calls to the same owner. */
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
