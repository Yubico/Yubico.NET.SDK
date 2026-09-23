#include "internal.h"
#include <IOKit/hid/IOHIDDevice.h>
#include <CoreFoundation/CoreFoundation.h>
#include <stdlib.h>

typedef struct {
    IOHIDDeviceRef device;
    dispatch_queue_t queue;
    uint8_t *report;
} iohid;
static void input(void *context, IOReturn status, void *sender, IOHIDReportType type,
                  uint32_t report_id, uint8_t *report, CFIndex length) {
    (void)sender;
    hidinput_iohid_input(context, status, type, report_id, report, length);
}
void hidinput_iohid_input(hidinput_owner *owner, IOReturn status, IOHIDReportType type,
                          uint32_t report_id, const uint8_t *report, CFIndex length) {
    if (status != kIOReturnSuccess || !report || length <= 0) {
        hidinput_report_fault(owner);
        return;
    }
    /* FIDO input uses unnumbered reports. Preserve the old connection's
       non-input/nonzero-ID ignore behavior rather than forwarding foreign frames. */
    if (type != kIOHIDReportTypeInput || report_id != 0) return;
    hidinput_incoming(owner, report, (size_t)length);
}
static void removed(void *context, IOReturn status, void *sender) {
    (void)status; (void)sender;
    if (status == kIOReturnSuccess) hidinput_removed(context);
    else hidinput_report_fault(context);
}
static void acknowledge(void *context) { hidinput_acked(context); }
static void register_all(hidinput_owner *o) {
    iohid *b = o->backend_data;
    IOHIDDeviceSetDispatchQueue(b->device, b->queue);
    IOHIDDeviceRegisterInputReportCallback(b->device, b->report, (CFIndex)o->max_report, input, o);
    IOHIDDeviceRegisterRemovalCallback(b->device, removed, o);
    IOHIDDeviceSetCancelHandler(b->device, ^{
        /* The following serial-queue item runs only after this handler returns.
           The callback buffer/device remain retained until that ack and delivery drain. */
        dispatch_async_f(b->queue, o, acknowledge);
    });
    IOHIDDeviceActivate(b->device);
}
static void cancel_backend(hidinput_owner *o) {
    iohid *b = o->backend_data;
    IOHIDDeviceCancel(b->device);
}
static hidinput_result try_release(hidinput_owner *o) {
    iohid *b = o->backend_data;
    if (o->started && IOHIDDeviceClose(b->device, kIOHIDOptionsTypeNone) != kIOReturnSuccess)
        return HIDINPUT_CLOSE_FAULT;
    CFRelease(b->device);
    dispatch_release(b->queue);
    free(b->report);
    free(b);
    return HIDINPUT_OK;
}
static const hidinput_backend backend = {register_all, cancel_backend, try_release};
hidinput_owner *hidinput_iohid_create(void *device, size_t max_report, size_t capacity,
                                      hidinput_receiver receiver, hidinput_terminal_cb terminal,
                                      void *context) {
    if (!device) return NULL;
    if (!terminal) return NULL;
    hidinput_owner *o = hidinput_allocate(max_report, capacity, receiver, terminal, context);
    if (!o) return NULL;
    iohid *b = calloc(1, sizeof(*b));
    if (!b) { hidinput_discard(o); return NULL; }
    o->backend_data = b;
    o->backend = &backend;
    b->report = malloc(max_report);
    b->queue = dispatch_queue_create("hidinput.iohid", DISPATCH_QUEUE_SERIAL);
    if (!b->report || !b->queue) {
        free(b->report);
        if (b->queue) dispatch_release(b->queue);
        free(b);
        hidinput_discard(o);
        return NULL;
    }
    CFRetain(device);
    b->device = device;
    return o;
}
