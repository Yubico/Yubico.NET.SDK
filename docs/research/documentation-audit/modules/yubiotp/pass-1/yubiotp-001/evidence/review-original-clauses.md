# Fresh reviewer original-source checks — 2026-11-04

Fresh retrieval hashes are in review-retrieval.json. All ten fetched sources matched pinned bytes;
no binary duplicate was saved. The Manual 3.4 404 is retained there. PDFs below were freshly
extracted from those hash-matched originals, not accepted from the investigator excerpt.

## vendor-technical-manual-v3.3.txt

```text
1:                   The YubiKey Manual
2: 
3: Usage, configuration and introduction of basic concepts
4:                                     Version: 3.3
5:                     Date: 17th September, 2014
6: 
7: The YubiKey Manual
8: 
9:              Disclaimer
10:              The contents of this document are subject to revision without notice due
11:              to continued progress in methodology, design, and manufacturing. Yubico
12:              shall have no liability for any error or damages of any kind resulting from
13:              the use of this document.
14:              The Yubico Software referenced in this document is licensed to you under
15:              the terms and conditions accompanying the software or as otherwise
16:              agreed between you or the company that you are representing.
17: 
18: 
```

```text
1173:                      user’s context. The 5 digits form a range with 100,000 unique
1174:                      combinations.
1175: 
1176: 
1177: 
1178: 
1179: © Yubico, 2015                           Page 26 of 41           Version: Yubikey Manual_V3.3.Doc
1180: 
1181: The YubiKey Manual
1182: 
1183: 5.5          The OTP string and the CFGFLAG_xx flags
1184: 
```

```text
1298: 
1299: 
1300: 5.5.11       CFGFLAG_MAN_UPDATE (from version 2.0)
1301:              In order to support legacy password systems, the YubiKey 2 supports
1302:              user-triggered static password change. The function is designed for the
1303:              specific use case of a traditional login system with a stricter password
1304:              policy where the user is asked to change their password on a regular
1305:              basis.
1306:              The intended use case is like the following:
1307:                  1. The user is asked to update their password.
1308:                  2. The user enters their secret password. The user presses the YubiKey
1309:                     button and the current fixed password is yielded
1310:                  3. The user is asked to enter the new password.
1311:                  4. The user enters their secret password. The user presses and holds
```

```text
1348: 
1349: © Yubico, 2015                           Page 29 of 41          Version: Yubikey Manual_V3.3.Doc
1350: 
1351: The YubiKey Manual
1352: 
1353: 5.5.14       CFGFLAG_OATH_FIXED_MODHEXx (from version 2.1)
1354:              These flags control the format of the Token Identifier string. It can either
1355:              be all numeric, the OMP Modhex, the OMP + TT Modhex or all Modhex.
1356:              CFGFLAG_OATH_FIXED_MODHEX1                      First byte is Modhex
1357:              CFGFLAG_OATH_FIXED_MODHEX2                      First two bytes are Modhex
1358:              CFGFLAG_OATH_FIXED_MODHEX                       All bytes are Modhex
1359: 
1360: 
1361: 5.5.15       CFGFLAG_CHAL_YUBICO (from version 2.2)
1362:              This flag set together with TKTFLAG_CHAL_RESP (5.1.9) enables Yubico
1363:              OTP challenge-response mode. When set, the configuration does not work
1364:              in normal OTP mode
1365: 
1366: 
1367: 5.5.16       CFGFLAG_CHALRESP_HMAC (from version 2.2)
1368:              This flag set together with TKTFLAG_CHAL_RESP (5.1.9) enables HMAC-
1369:              SHA1 challenge-response mode. When set, the configuration does not
1370:              work in normal OTP mode
1371: 
1372: 
1373: 5.5.17       CFGFLAG_CHALRESP_BTN_TRIG (from version 2.2)
1374:              This flag is not a format flag but is included here for backwards
1375:              compatibility. When set together with the TKTFLAG_CHAL_RESP flag,
1376:              the challenge-response configuration requires user acceptance by touching
1377:              the YubiKey button.
1378: 
1379: 5.6          The EXTFLAG_xx extended mode flags
1380:              Additional extended properties are controlled by the EXTFLAG_xx
1381:              settings. These are binary flags that can be turned either on or off.
1382:              Extended flags that are device specific rather than configuration specific
1383:              (SERIAL_BTN_VISIBLE, SERIAL_USB_VISIBLE, SERIAL_API_VISIBLE) are
1384:              merged, i.e. if a flag is set in any of the configuration, the device consider
1385:              the flag to be set.
1386: 
1387: 
1388: 5.6.1        EXTFLAG_SERIAL_BTN_VISIBLE (from version 2.2)
1389:              This flag allows the serial number to be retrieved by holding the touch
1390:              button while inserting the device into the USB port. Once the LED starts to
1391:              flash, release the button and the serial number will then be sent as a
1392:              string of digits.
1393: 
1394: 
1395: 5.6.2        EXTFLAG_SERIAL_USB_VISIBLE (from version 2.2)
1396:              This flag makes the serial number to appear in the USB descriptor's
1397:              iSerialNumber field. Note that this makes each device unique from the
1398:              host computer's view.
1399: 
1400: 
1401: 5.6.3        EXTFLAG_SERIAL_API_VISIBLE (from version 2.2)
1402:              This flag allows the serial number to be read via proprietary API calls
1403: 
1404: 
1405: 
1406: 
1407: © Yubico, 2015                           Page 30 of 41           Version: Yubikey Manual_V3.3.Doc
1408: 
1409: The YubiKey Manual
1410: 
1411: 5.6.4        EXTFLAG_USE_NUMERIC_KEYPAD (from version 2.3)
1412:              In OATH-HOTP mode, numeric digits are sent rather than Modhex
1413:              characters. This may cause problems with certain keyboard layouts.
1414:              Setting the USE_NUMERIC_KEYPAD flag causes numeric character to be
1415:              sent as keystrokes from the numeric keypad rather than the normal
1416:              numeric keys on a 84-key keyboard.
1417: 
1418: 
1419: 5.6.5        EXTFLAG_FAST_TRIG (from version 2.3)
1420:              Setting this flag causes the trigger action to become faster. It only applies
1421:              when one configuration is written. If both configurations are set, the flag
1422:              has no effect.
1423: 
1424: 
1425: 5.6.6        EXTFLAG_ALLOW_UPDATE (from version 2.3)
1426:              Normally, a configuration has to be entirely re-written if anything is to be
1427:              changed. The ALLOW_UPDATE flag allows certain non-security related
1428:              flags to be modified after the configuration has been written. These flags
1429:              are:
1430:              TKTFLAG_TAB_FIRST
1431:              TKTFLAG_APPEND_TAB1
1432:              TKTFLAG_APPEND_TAB2
1433:              TKTFLAG_APPEND_DELAY1
1434:              TKTFLAG_APPEND_DELAY2
1435:              TKTFLAG_APPEND_CR
1436:              CFGFLAG_PACING_10MS
1437:              CFGFLAG_PACING_20MS
1438:              EXTFLAG_SERIAL_BTN_VISIBLE
1439:              EXTFLAG_SERIAL_USB_VISIBLE
1440:              EXTFLAG_SERIAL_API_VISIBLE
1441:              EXTFLAG_USE_NUMERIC_KEYPAD
1442:              EXTFLAG_FAST_TRIG
1443:              EXTFLAG_ALLOW_UPDATE
1444:              EXTFLAG_DORMANT
1445:              EXTFLAG_LED_INV
1446:              It is important to keep the EXTFLAG_DORMANT flag set when updating the
1447:              configuration if the desire is to keep it on. Once cleared, the flag cannot
1448:              be set without a complete re-write of the configuration.
1449:              The flag can be combined with a write protection (see section 5.7)
1450: 
```

```text
1480:              code.
1481: 
1482: 5.8          Automatic navigation
1483:              In YubiKey 1, prior to version 1.3.5, a function was provided to allow
1484:              automatic navigation when the device is inserted, where an URL string
1485:              was outputted. This function is discouraged and has been removed in
1486:              recent versions as it implies potential security and compatibility issues.
1487: 
1488: 
1489: 
1490: 
1491: © Yubico, 2015                         Page 32 of 41          Version: Yubikey Manual_V3.3.Doc
1492: 
1493: The YubiKey Manual
1494: 
```

```text
1557:              practical to use in challenge-response settings where the interaction
1558:              occurs very often. Consider HMAC-SHA1 mode instead for such settings.
1559:              The field is stored in little-endian format, i.e. the least significant byte
1560:              being stored first.
1561: 
1562: 
1563: 6.1.3        Timestamp
1564:              The timestamp is a 24-bit field incremented with a rate of approximately 8
1565:              Hz. The timestamp value is set to a random value after startup from the
1566:              internal random number generator.
1567:              This field may be used by the verifying party to determine the time
```

```text
1736:              Detailed examples and information of the implementation can be found in
1737:              published source code libraries, accessible via the Yubico developer’s web
1738:              page.
1739: 
1740: 
1741: 
1742: 
1743: © Yubico, 2015                         Page 37 of 41          Version: Yubikey Manual_V3.3.Doc
1744: 
1745: The YubiKey Manual
1746: 
1747: 
1748: 7            The YubiKey NEO
1749:              The YubiKey NEO is an enhanced version of the standard YubiKey,
1750:              featuring the following additional features
1751:                     Backwards-compatible with standard YubiKey
1752:                     Comprises a Common Criteria EAL5+ compliant secure element for
```

## personalization-1.20.0-manual.txt

```text
59: *uid*[='uuuuuu']::: The uid part of the generated OTP, also called __private identity__, in hex. Must be 12 characters long. The uid is 6 bytes of static data that is included (encrypted) in every OTP, and is used to validate that an OTP was in fact encrypted with the AES key shared between the YubiKey and the validation service. It cannot be used to identify the YubiKey as it is only readable to those that know the AES key. If no argument is provided the uid is prompted for on stdin.
60: 
61: *access*[='fffffffffff']::: New hex access code to set. Must be 12 characters long. If an access code is set, it will be required for subsequent reprogramming of the YubiKey. If no argument is provided code is prompted for on stdin.
62: 
63: *oath-imf*='xxx'::: Set OATH Initial Moving Factor. This is the initial counter value for the YubiKey. This should be a value between 0 and 1048560, evenly dividable by 16.
64: 
65: [-]'ticket-flag'::: Set/clear ticket flag, see the section link:#ticket-flags['Ticket Flags'].
```

```text
104: or u2f-host (for U2F mode).
105: 
106: === YubiKey 3 and above
107: 
108: *-S*'0605...'::
109: 
110: set the scanmap to be used with the YubiKey. It must be 45 unique
111: bytes as 90 characters. Leave argument empty to reset to the YubiKey’s
112: default. The scanmap must be sent in the order:
113:  
114:  cbdefghijklnrtuvCBDEFGHIJKLNRTUV0123456789!\t\r
115: +
116: The default scanmap in the YubiKey is:
```

```text
277: [-]*chal-hmac*::
278: 
279: Generate HMAC-SHA1 challenge responses.
280: 
281: [-]*hmac-lt64*::
282: 
283: Calculate HMAC on less than 64 bytes input. Whatever is in the last byte
284: of the challenge is used as end of input marker (backtracking from end
285: of payload).
286: 
287: [-]*chal-btn-trig*::
288: 
289: The YubiKey will wait for the user to press the key (within 15 seconds)
290: before answering the challenge.
291: 
292: [-]*serial-btn-visible*::
293: 
294: The YubiKey will emit its serial number if the button is pressed during
295: power-up. This option is only valid for the 2.x firmware line.
296: 
297: [-]*serial-usb-visible*::
298: 
299: The YubiKey will indicate its serial number in the USB iSerial field.
300: This option is not available in the 3.0 and 3.1 firmwares.
301: 
302: [-]*serial-api-visible*::
```

```text
324: 
325: === YubiKey 2.3 firmware and above
326: [-]*use-numeric-keypad*::
327: 
328: Send scancodes for numeric keypad keypresses when sending digits - helps
329: with some keyboard layouts. This option is only valid for the 2.x
330: firmware line.
331: 
332: [-]*fast-trig*::
333: 
334: Faster triggering when only configuration 1 is available. This option is
335: always in effect on firmware versions 3.0 and above.
336: 
337: [-]*allow-update*::
338: 
```

## personalization-1.20.0-ykpers.c

```text
730: int ykp_set_oath_imf(YKP_CONFIG *cfg, unsigned long imf)
731: {
732: 	if (!capability_has_oath_imf(cfg)) {
733: 		ykp_errno = YKP_EYUBIKEYVER;
734: 		return 0;
735: 	}
736: 	if (imf > 65535*16) {
737: 		ykp_errno = YKP_EINVAL;
738: 		return 0;
739: 	}
740: 	if (imf % 16 != 0) {
741: 		ykp_errno = YKP_EINVAL;
742: 		return 0;
743: 	}
744: 	/* IMF/16 is 16 bits stored big-endian in uid[4] */
745: 	imf /= 16;
746: 	cfg->ykcore_config.uid[4] = imf >> 8;
747: 	cfg->ykcore_config.uid[5] = imf;
748: 	return 1;
749: }
750: 
751: unsigned long ykp_get_oath_imf(const YKP_CONFIG *cfg)
752: {
753: 	if (!capability_has_oath_imf(cfg)) {
754: 		return 0;
755: 	}
756: 
757: 	/* we can't do a simple cast due to alignment issues */
758: 	return ((cfg->ykcore_config.uid[4] << 8)
759: 		| cfg->ykcore_config.uid[5]) << 4;
760: }
```

## personalization-1.20.0-ykdef.h

```text
105: #define	KEY_SIZE_OATH		20      /* Size of OATH-HOTP key (key field + first 4 of UID field) */
106: #define	ACC_CODE_SIZE		6	/* Size of access code to re-program device */
107: 
108: struct config_st {
109: 	unsigned char fixed[FIXED_SIZE];/* Fixed data in binary format */
110: 	unsigned char uid[UID_SIZE];	/* Fixed UID part of ticket */
111: 	unsigned char key[KEY_SIZE];	/* AES key */
112: 	unsigned char accCode[ACC_CODE_SIZE]; /* Access code to re-program device */
113: 	unsigned char fixedSize;	/* Number of bytes in fixed field (0 if not used) */
114: 	unsigned char extFlags;		/* Extended flags - YubiKey 2.? and above */
115: 	unsigned char tktFlags;		/* Ticket configuration flags */
```

```text
125: #define	TKTFLAG_APPEND_TAB1	0x02	/* Send TAB after first part */
126: #define	TKTFLAG_APPEND_TAB2	0x04	/* Send TAB after second part */
127: #define	TKTFLAG_APPEND_DELAY1	0x08	/* Add 0.5s delay after first part */
128: #define	TKTFLAG_APPEND_DELAY2	0x10	/* Add 0.5s delay after second part */
129: #define	TKTFLAG_APPEND_CR	0x20	/* Append CR as final character */
130: 
131: /* Yubikey 2 and above */
132: #define TKTFLAG_PROTECT_CFG2	0x80	/* Block update of config 2 unless config 2 is configured and has this bit set */
133: 
134: /* Configuration flags *******************************************************/
135: 
136: /* Yubikey 1 and above */
137: #define CFGFLAG_SEND_REF	0x01	/* Send reference string (0..F) before data */
138: #define CFGFLAG_PACING_10MS	0x04	/* Add 10ms intra-key pacing */
139: #define CFGFLAG_PACING_20MS	0x08	/* Add 20ms intra-key pacing */
```

```text
180: 
181: #define EXTFLAG_LED_INV             0x80        /* LED idle state is off rather than on */
182: 
183: /* Flags valid for update */
184: 
185: #define TKTFLAG_UPDATE_MASK         (TKTFLAG_TAB_FIRST | TKTFLAG_APPEND_TAB1 | TKTFLAG_APPEND_TAB2 | TKTFLAG_APPEND_DELAY1 | TKTFLAG_APPEND_DELAY2 | TKTFLAG_APPEND_CR)
186: #define CFGFLAG_UPDATE_MASK         (CFGFLAG_PACING_10MS | CFGFLAG_PACING_20MS)
187: #define EXTFLAG_UPDATE_MASK         (EXTFLAG_SERIAL_BTN_VISIBLE | EXTFLAG_SERIAL_USB_VISIBLE |  EXTFLAG_SERIAL_API_VISIBLE | EXTFLAG_USE_NUMERIC_KEYPAD | EXTFLAG_FAST_TRIG | EXTFLAG_ALLOW_UPDATE | EXTFLAG_DORMANT | EXTFLAG_LED_INV)
188: 
```

```text
246: #define DEFAULT_CHAL_TIMEOUT	15	/* Default challenge timeout in seconds */
247: 
248: /* Scancode mapping (version 3.0) */
249: 
250: #define SCAN_MAP		"cbdefghijklnrtuvCBDEFGHIJKLNRTUV0123456789!\t\r"
251: #define SHIFT_FLAG		0x80	/* Flag for shifted scan codes */
252: 
```

## ykman-5.8.0-yubiotp.py

```text
370: class HmacSha1SlotConfiguration(SlotConfiguration):
371:     def __init__(self, key: bytes):
372:         super(HmacSha1SlotConfiguration, self).__init__()
373: 
374:         key = _shorten_hmac_key(key)
375: 
376:         # Key is packed into key and uid
377:         self._key = key[:KEY_SIZE].ljust(KEY_SIZE, b"\0")
378:         self._uid = key[KEY_SIZE:].ljust(UID_SIZE, b"\0")
379: 
380:         self._update_flags(TKTFLAG.CHAL_RESP, True)
381:         self._update_flags(CFGFLAG.CHAL_HMAC, True)
382:         self._update_flags(CFGFLAG.HMAC_LT64, True)
383: 
384:     def is_supported_by(self, version):
385:         return version >= (2, 2, 0)
386: 
387:     def require_touch(self: Cfg, value: bool) -> Cfg:
388:         self._update_flags(CFGFLAG.CHAL_BTN_TRIG, value)
389:         return self
390: 
391:     def lt64(self: Cfg, value: bool) -> Cfg:
392:         self._update_flags(CFGFLAG.HMAC_LT64, value)
393:         return self
394: 
```

```text
454:     def imf(self: Cfg, imf: int) -> Cfg:
455:         if not (imf % 16 == 0 and 0 <= imf <= 0xFFFF0):
456:             raise ValueError(
457:                 f"imf should be between {0} and {1048560}, evenly dividable by 16"
458:             )
459:         self._uid = self._uid[:4] + struct.pack(">H", imf >> 4)
460:         return self
461: 
462: 
```

```text
609:     def is_touch_triggered(self, slot: SLOT) -> bool:
610:         """Checks if a (programmed) state is triggered by touch (not challenge-response)
611:         Requires YubiKey 3 or later.
612:         """
613:         require_version(self.version, (3, 0, 0))
614:         return self.flags & (CFGSTATE.SLOT1_TOUCH, CFGSTATE.SLOT2_TOUCH)[slot - 1] != 0
615: 
```

```text
820:     def update_configuration(
821:         self,
822:         slot: SLOT,
823:         configuration: SlotConfiguration,
824:         acc_code: bytes | None = None,
825:         cur_acc_code: bytes | None = None,
826:     ) -> None:
827:         """Update configuration in slot.
828: 
829:         :param slot: The slot to update the configuration in.
830:         :param configuration: The slot configuration.
831:         :param acc_code: The new access code.
832:         :param cur_acc_code: The current access code.
833:         """
834:         if not configuration.is_supported_by(self.version):
835:             raise NotSupportedError(
836:                 "This configuration is not supported on this YubiKey version"
837:             )
838:         if acc_code != cur_acc_code and (4, 3, 2) <= self.version < (4, 3, 6):
839:             raise NotSupportedError(
840:                 "The access code cannot be updated on this YubiKey. "
841:                 "Instead, delete the slot and configure it anew."
842:             )
843:         slot = SLOT(slot)
844:         logger.debug(f"Writing configuration update to slot {slot}")
845:         self._write_config(
846:             SLOT.map(slot, CONFIG_SLOT.UPDATE_1, CONFIG_SLOT.UPDATE_2),
847:             configuration.get_config(acc_code),
848:             cur_acc_code,
849:         )
850: 
```

```text
901:     def calculate_hmac_sha1(
902:         self,
903:         slot: SLOT,
904:         challenge: bytes,
905:         event: Event | None = None,
906:         on_keepalive: Callable[[int], None] | None = None,
907:     ) -> bytes:
908:         """Perform a challenge-response operation using HMAC-SHA1.
909: 
910:         :param slot: The slot to perform the operation against.
911:         :param challenge: The challenge.
912:         :param event: An event.
913:         """
914:         require_version(self.version, (2, 2, 0))
915:         slot = SLOT(slot)
916:         logger.debug(f"Calculating response for slot {slot}")
917: 
918:         # Pad challenge with byte different from last
919:         challenge = challenge.ljust(
920:             HMAC_CHALLENGE_SIZE, b"\1" if challenge.endswith(b"\0") else b"\0"
921:         )
922:         return self.backend.send_and_receive(
923:             SLOT.map(slot, CONFIG_SLOT.CHAL_HMAC_1, CONFIG_SLOT.CHAL_HMAC_2),
924:             challenge,
925:             HMAC_RESPONSE_SIZE,
926:             event,
927:             on_keepalive,
928:         )
```

## nfc-uri-rtd-1.0-mirror.txt

```text
243: 3.2 Records Mapping
244: 3.2.1 URI Record Type
245: The Well Known Type for an URI record is “U” (0x55 in the NDEF binary representation).
246: The structure of an URI record is described below.
247: 
248:                                   Table 2. URI Record Contents
249: 
250:         Name         Offset      Size       Value                  Description
251:         Identifier   0           1 byte     URI identifier code    The URI identifier code, as
252:         code                                                       specified in Table 3.
253:         URI          1           N          UTF-8 string           The rest of the URI, or the entire
254:         field                                                      URI (if identifier code is 0x00).
255: 
256: 3.2.2 URI Identifier Code
257: In order to shorten the URI, the first byte of the record data describes the protocol field of an
258: URI. The following table MUST be used to encode and decode the URI, though applications
259: MAY use the 0x00 value to denote no prefixing when encoding, regardless of whether there
260: actually is a suitable abbreviation code.
261: For explanations of the different protocols, please refer to the protocol documentations
262: themselves. NFC devices are not required to support any particular protocol.
263: 
264:                                      Table 3. Abbreviation Table
265: 
266:           Decimal        Hex              Protocol
267:           0              0x00             N/A. No prepending is done, and the URI field
268:                                           contains the unabridged URI.
269:           1              0x01             http://www.
270:           2              0x02             https://www.
271:           3              0x03             http://
272:           4              0x04             https://
273:           5              0x05             tel:
274:           6              0x06             mailto:
275:           7              0x07             ftp://anonymous:anonymous@
276:           8              0x08             ftp://ftp.
277:           9              0x09             ftps://
278: 
279: 
280: 
281: URI Record Type Definition                                                                     Page 5
282: 
283:                                                                                     NDEF Structure
284: 
285: 
286:           Decimal     Hex                Protocol
287:           10          0x0A               sftp://
288:           11          0x0B               smb://
289:           12          0x0C               nfs://
290:           13          0x0D               ftp://
291:           14          0x0E               dav://
292:           15          0x0F               news:
293:           16          0x10               telnet://
294:           17          0x11               imap:
295:           18          0x12               rtsp://
296:           19          0x13               urn:
297:           20          0x14               pop:
298:           21          0x15               sip:
299:           22          0x16               sips:
300:           23          0x17               tftp:
301:           24          0x18               btspp://
302:           25          0x19               btl2cap://
303:           26          0x1A               btgoep://
304:           27          0x1B               tcpobex://
305:           28          0x1C               irdaobex://
306:           29          0x1D               file://
307:           30          0x1E               urn:epc:id:
308:           31          0x1F               urn:epc:tag:
309:           32          0x20               urn:epc:pat:
310:           33          0x21               urn:epc:raw:
311:           34          0x22               urn:epc:
312:           35          0x23               urn:nfc:
313:           36…255      0x24..0xFF         RFU
314: 
315: For example, if the content of this field is 0x02, and the content of the URI field reads as “nfc-
316: forum.org”, the resulting URI is “https://www.nfc-forum.org”.
317: If the content this field is zero (0x00), then NO prepending SHALL be done.
318: All fields marked RFU SHALL be treated as if they were value zero (no prepending). A
319: compliant system MUST NOT produce values that are marked RFU.
320: 
321: 
322: 
323: URI Record Type Definition                                                                    Page 6
324: 
325:                                                                                    NDEF Structure
326: 
327: 
328: 3.2.3 URI Field
329: This field provides the URI as per RFC 3987 [RFC 3987] (so that it is actually an IRI, or
330: Internationalized Resource Identifier, but for legacy reasons we use the word URI). This IRI can
331: be a URL or URN as explained before. The encoding used MUST be UTF-8, unless the URI
332: scheme specifies some particular encoding.
333: The length of the IRI can be calculated by taking the length of the payload, and subtracting 1 for
334: the protocol abbreviation code byte. This is the length in bytes, not in characters (as UTF-8
335: characters can occupy more than one byte).
336: URIs are defined only in the 7-bit US-ASCII space. Therefore, a compliant application SHOULD
337: transform the UTF-8 IRI string to a 7-bit US-ASCII string by changing code points above 127
338: into the proper encoding. This coding has been defined in the RFC 3987 [RFC 3987] and IDN
339: [RFC 3492] documents. For different schemes, the encoding may be different.
340: For example, if the URI (after the prepending of the URI type field) contains the following string:
341: “http://www.hääyö.com/”, it is transformed, as per standard IDN [RFC 3492] rules, into
342: “http://www.xn--hy-viaa5g.com” before acting on it. Most modern applications already support
343: this new Internationalized Resource Identifier (IRI) scheme. It is RECOMMENDED that
344: implementations include support for IRI where display of the URI in human-readable form is
345: anticipated.
346: To clarify: yes, the URI MAY contain UTF-8 characters. However, the Internet cannot handle
347: them, and therefore the URI needs to be transformed before use. For most devices, this
348: conversion is handled by the application.
349: Any character value within the URI between (and including) 0 and 31 SHALL be recorded as an
350: error, and the URI record to be discarded. Any invalid UTF-8 sequence SHALL be considered an
351: error, and the entire URI record SHALL be discarded.
352: 
353: 
354: 
355: 
356: URI Record Type Definition                                                                  Page 7
357: 
358:                                                                               Handling Guideline
359: 
360: 
454: B. Revision History
455: The following table outlines the revision history of the RTD_URI Technical Specification.
456: 
457:                                     Table 7. Revision History
458: 
459: Document           Revision and         Status             Change Notice       Supersedes
460: Name               Release Date
461: NFCForum-TS-       1.0, July 2006       Final              None
462: RTD_URI_1.0
463: 
464: 
465: 
466: 
467: URI Record Type Definition                                                              Page 11
468: 
```

## nfc-text-rtd-1.0-mirror.txt

```text
223: 3.2 Records Mapping
224: 3.2.1 Syntax
225: The NFC Forum Well Known Type [NDEF], [NFC RTD] for the Text record is “T” (in NFC
226: binary encoding: 0x54).
227: The data content is as follows:
228: 
229:                               Table 2. Text Record Content Syntax
230: 
231:                Offset     Length      Content
232:                (bytes)    (bytes)
233:                0          1           Status byte. See Table 3.
234: 
235:                1          <n>         ISO/IANA language code. Examples: “fi”, “en-US”, “fr-
236:                                       CA”, “jp”. The encoding is US-ASCII.
237:                n+1        <m>         The actual text. Encoding is either UTF-8 or UTF-16,
238:                                       depending on the status bit.
239: 
240: The Status bit encodings are as described in Table 3. Any value marked RFU SHALL be ignored,
241: and any software writing these bits SHALL use the value zero for these bits.
242: 
243:                                   Table 3. Status Byte Encodings
244: 
245:                Bit number (0      Content
246:                is LSB)
247: 
248:                7                  0: The text is encoded in UTF-8
249:                                   1: The text is encoded in UTF16
250: 
251:                6                  RFU (MUST be set to zero)
252: 
253: 
254:                5..0               The length of the IANA language code.
255: 
256: 
257: The contents of the text field MAY be shown to the user. If multiple 'T' records exist, the one
258: with the closest matching language to the user preference SHOULD be displayed. To have
259: multiple text elements within a single application, context with the same language code SHOULD
260: be considered an error.
261: 
262: 
263: 
264: 
265: Text Record Type Definition                                                                  Page 4
266: 
267:                                                                                       NDEF structure
268: 
269: 
270: Control characters (0x00-0x1F in UTF-8) should be removed prior to display, except for newline,
271: line feed (0x0D, 0x0A) and tab (0x08) characters. Markup MUST NOT be embedded (please use
272: the “text/xhtml” or other suitable MIME types). The Text record should be considered to be equal
273: to the MIME type “text/plain; format=fixed”.
274: Line breaks in the text MUST be represented using the CRLF (so-called DOS convention, the
275: sequence 0x0D,0x0A in UTF-8). The device may deal with the tab character as it wishes.
276: White space other than newline and tab SHOULD be collapsed, i.e., multiple space characters are
277: to be considered a single space character.
278: To find the length of the actual text in bytes, you calculate the length via “m=(length of the
279: payload – length of the IANA language code – 1)”
280: 
281: 3.2.2 Structure
282: If the Text record describes an element, it SHOULD occur in the NDEF record list before the
283: element it is describing. This makes it faster to find and display to the user if the element is very
284: large.
285: 
286: 3.3 Language Codes
287: All language codes MUST be done according to RFC 3066 [RFC3066]. The language code MAY
288: NOT be omitted.
289: The language code length is encoded in the six least significant bits of the status byte. Thus it is
290: easy to find by masking the status byte with the value 0x3F.
291: The language code is typically either two characters or five characters, though in the future, it is
292: likely that it will be possible to have longer codes. At this time, IETF is considering an extension
293: to RFC 3066 which will cover language codes up to 33 bytes in length [RFC 3066bis]. The two-
294: character version disregards any dialects, and thus is used most often; for example, “fi” for
295: Finnish, “jp” for Japanese, “fr” for French. However, in some cases you might want to
296: differentiate between variants of the same language, such as providing US-English and British
297: English versions via “en-US” and “en-UK” respectively.
298: 
299: 3.4 UTF-16 Byte Order
300: The Unicode Byte-Order-Mark (BOM) in the actual string MUST be tolerated (i.e. no error
301: condition). When generating a Text record, the BOM MAY be omitted. If the BOM is omitted,
302: the byte order shall be big-endian (UTF-16 BE).
303: 
304: 
305: 
306: 
307: Text Record Type Definition                                                                      Page 5
308: 
309:                                                                        Example UTF-8 Encoding
310: 
311: 
344: B. Revision History
345: The following table outlines the revision history of the Text RTD Technical Specification.
346: 
347:                                      Table 5. Revision History
348: 
349: Document            Revision and         Status             Change Notice       Supersedes
350: Name                Release Date
351: NFCForum-TS-        1.0, July 2006                          None                First Revision
352: RTD_Text_1.0
353: 
354: 
355: 
356: 
357: Text Record Type Definition                                                                  Page 7
358: 
```

## Extraction commands and original diagnostics

```json
[
  {
    "source": "vendor-technical-manual-v3.3.pdf",
    "command": "pdftotext -layout <pinned-original> -",
    "exit": 0,
    "stderr": "",
    "extracted_sha256": "ba1462dfd6285c610e4e0b5f315a0f4c1073223aa89e63aa0e477c81cb87d37c",
    "matches_investigator_text": true
  },
  {
    "source": "nfc-uri-rtd-1.0-mirror.pdf",
    "command": "pdftotext -layout <pinned-original> -",
    "exit": 0,
    "stderr": "Syntax Error: Expected the default config, but wasn't able to find it, or it isn't a Dictionary\n",
    "extracted_sha256": "e7f0648910e56861e39a81147d906d221808f94ce25d3bfc21e96d82636f20c5",
    "matches_investigator_text": true
  },
  {
    "source": "nfc-text-rtd-1.0-mirror.pdf",
    "command": "pdftotext -layout <pinned-original> -",
    "exit": 0,
    "stderr": "Syntax Error: Expected the default config, but wasn't able to find it, or it isn't a Dictionary\n",
    "extracted_sha256": "30f06fb5ce9e9b03236709caf7f5cb9480d99900a4143ba4de7752ea38f123ee",
    "matches_investigator_text": true
  }
]
```

## Fresh Context7 check

Resolved .NET to `/dotnet/docs` and queried ownership/lifetime and immutable strings.
Returned https://github.com/dotnet/docs/blob/main/docs/csharp/fundamentals/strings/index.md
(created string characters cannot change) and
https://github.com/dotnet/docs/blob/main/docs/standard/memory-and-spans/memory-t-usage-guidelines.md
(single ownership or transfer; async memory consumption ends at terminal Task state).
These do not prove SDK output ownership; that conclusion uses module/backend return paths.
