#include "owner.h"

/* macOS-only shipping interop contract. Synthetic entry points are not linked. */
hidinput_owner *Native_HidInputCreate(void *already_open_device, size_t max_report,
    size_t capacity, hidinput_receiver report_cb, hidinput_terminal_cb terminal_cb,
    void *context) {
    return hidinput_iohid_create(already_open_device, max_report, capacity,
                                report_cb, terminal_cb, context);
}
int Native_HidInputStart(hidinput_owner *owner) { return hidinput_start(owner); }
void Native_HidInputCancel(hidinput_owner *owner) { hidinput_cancel(owner); }
int Native_HidInputWaitShutdown(hidinput_owner *owner, uint32_t timeout_ms) {
    return hidinput_wait_shutdown(owner, timeout_ms);
}
int Native_HidInputDestroy(hidinput_owner *owner) { return hidinput_destroy(owner); }
