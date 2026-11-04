/*
 * Copyright 2015-2018 Yubico AB
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <stdint.h>

#include "internal.h"
#include "ykhsmauth.h"

#ifdef _WIN32
#define strncasecmp _strnicmp
#endif

static uint16_t encode_len(uint8_t *buf, uint16_t len) {
  if (len > 0xff) {
    *buf++ = 0x82;
    *buf++ = len >> 8;
    *buf++ = len;
    return 3;
  }
  if (len > 0x7f) {
    *buf++ = 0x81;
    *buf++ = len;
    return 2;
  }
  *buf++ = len;
  return 1;
}

static void add_tag(APDU *apdu, uint8_t tag, const void *data, uint16_t len,
                    uint16_t pad) {
  uint8_t *ptr = apdu->st.data + apdu->st.lc;
  *ptr++ = tag;
  ptr += encode_len(ptr, len + pad);
  memcpy(ptr, data, len);
  ptr += len;
  memset(ptr, 0, pad);
  ptr += pad;
  apdu->st.lc = ptr - apdu->st.data;
}

static ykhsmauth_rc translate_error(uint16_t sw, uint8_t *retries) {
  if ((sw & 0xfff0) == SW_AUTHENTICATION_FAILED) {
    if (retries != NULL) {
      *retries = sw & ~0xfff0;
    }
    return YKHSMAUTHR_WRONG_PW;
  } else if (sw == SW_FILE_FULL) {
    return YKHSMAUTHR_STORAGE_FULL;
  } else if (sw == SW_FILE_NOT_FOUND) {
    return YKHSMAUTHR_ENTRY_NOT_FOUND;
  } else if (sw == SW_WRONG_DATA) {
    return YKHSMAUTHR_INVALID_PARAMS;
  } else if (sw == SW_MEMORY_ERROR) {
    return YKHSMAUTHR_MEMORY_ERROR;
  } else if (sw == SW_SECURITY_STATUS_NOT_SATISFIED) {
    return YKHSMAUTHR_TOUCH_ERROR;
  } else if (sw == SW_FILE_INVALID) {
    return YKHSMAUTHR_ENTRY_INVALID;
  } else if (sw == SW_DATA_INVALID) {
    return YKHSMAUTHR_DATA_INVALID;
  } else if (sw == SW_INS_NOT_SUPPORTED) {
    return YKHSMAUTHR_NOT_SUPPORTED;
  } else {
    return YKHSMAUTHR_GENERIC_ERROR;
  }
}

ykhsmauth_rc ykhsmauth_init(ykhsmauth_state **state, int verbose) {
  if (state == NULL) {
    if (verbose) {
      fprintf(stderr, "Unable to initialize: %s\n",
              ykhsmauth_strerror(YKHSMAUTHR_INVALID_PARAMS));
    }

    return YKHSMAUTHR_INVALID_PARAMS;
  }

  ykhsmauth_state *s = calloc(1, sizeof(ykhsmauth_state));

  if (s == NULL) {
    if (verbose) {
      fprintf(stderr, "Unable to initialize: %s\n",
              ykhsmauth_strerror(YKHSMAUTHR_MEMORY_ERROR));
    }

    return YKHSMAUTHR_MEMORY_ERROR;
  }

  s->verbose = verbose;
  *state = s;

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_done(ykhsmauth_state *state) {
  ykhsmauth_disconnect(state);

  if (state == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  SCardReleaseContext(state->context);
  state->context = 0;
  free(state);

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_disconnect(ykhsmauth_state *state) {
  if (state == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  if (state->card) {
    SCardDisconnect(state->card, SCARD_RESET_CARD);
    state->card = 0;
  }

  return YKHSMAUTHR_SUCCESS;
}

static void dump_hex(const unsigned char *buf, DWORD len) {
  for (DWORD i = 0; i < len; i++) {
    fprintf(stderr, "%02x ", buf[i]);
  }
}

static ykhsmauth_rc send_data(ykhsmauth_state *state, const APDU *apdu,
                              unsigned char *data, LPDWORD recv_len,
                              uint16_t *sw) {
  unsigned char apdu_bytes[sizeof(apdu->raw)] = {0};
  unsigned char get_response_apdu[5] = {0x00, 0xC0, 0x00, 0x00, 0x00};
  DWORD send_len = 0;
  DWORD total_recv_len = 0;
  DWORD chunk_len = 0;
  unsigned char *recv_ptr = NULL;
  DWORD temp_recv_len = 0;

  *sw = 0;

  if (apdu->st.lc > sizeof(apdu->st.data)) {
    if (state->verbose) {
      fprintf(stderr, "APDU too large: lc=%u\n", (unsigned) apdu->st.lc);
    }
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  apdu_bytes[0] = apdu->st.cla;
  apdu_bytes[1] = apdu->st.ins;
  apdu_bytes[2] = apdu->st.p1;
  apdu_bytes[3] = apdu->st.p2;

  if (apdu->st.lc <= 255) {
    apdu_bytes[4] = (unsigned char) apdu->st.lc;
    memcpy(apdu_bytes + 5, apdu->st.data, apdu->st.lc);
    send_len = (DWORD) apdu->st.lc + 5;
  } else {
    apdu_bytes[4] = 0x00; // Extended length marker
    apdu_bytes[5] = (unsigned char) (apdu->st.lc >> 8);   // Length high byte
    apdu_bytes[6] = (unsigned char) (apdu->st.lc & 0xff); // Length low byte
    memcpy(apdu_bytes + 7, apdu->st.data, apdu->st.lc);
    send_len = (DWORD) apdu->st.lc + 7;
  }

  if (state->verbose > 1) {
    fprintf(stderr, "> ");
    dump_hex(apdu_bytes, send_len);
    fprintf(stderr, "\n");
  }

  // Send initial command
  recv_ptr = data;
  chunk_len = *recv_len;

  int32_t rc = SCardBeginTransaction(state->card);
  if (rc != SCARD_S_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "SCardBeginTransaction failed, rc=%08x\n", rc);
    }
    SCardEndTransaction(state->card, SCARD_LEAVE_CARD);
    return YKHSMAUTHR_PCSC_ERROR;
  }


  rc = SCardTransmit(state->card, SCARD_PCI_T1, apdu_bytes, send_len,
                             NULL, recv_ptr, &chunk_len);

  if (rc != SCARD_S_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "SCardTransmit failed, rc=%08x\n", rc);
    }
    SCardEndTransaction(state->card, SCARD_LEAVE_CARD);
    return YKHSMAUTHR_PCSC_ERROR;
  }

  if (chunk_len < 2) {
    if (state->verbose) {
      fprintf(stderr, "Response too short: %lu bytes\n", (unsigned long) chunk_len);
    }
    SCardEndTransaction(state->card, SCARD_LEAVE_CARD);
    return YKHSMAUTHR_GENERIC_ERROR;
  }

  total_recv_len = chunk_len;
  *sw = (recv_ptr[chunk_len - 2] << 8) | recv_ptr[chunk_len - 1];

  if (state->verbose > 1) {
    fprintf(stderr, "< ");
    dump_hex(recv_ptr, chunk_len);
    fprintf(stderr, "\n");
    fprintf(stderr, "SW: %04x\n", *sw);
  }

  // Handle APDU chaining: 61 XX means "more data available"
  while ((*sw & 0xFF00) == 0x6100) {
    uint8_t le_byte = (*sw) & 0x00FF;  // Extract XX from 61 XX

    if (le_byte == 0) {
      le_byte = 0x00;  // 61 00 means query card for available length
    }

    // Check if we have buffer space for another chunk
    if (total_recv_len >= *recv_len) {
      if (state->verbose) {
        fprintf(stderr, "Buffer overflow: received %lu, max %lu\n",
                (unsigned long) total_recv_len, (unsigned long) *recv_len);
      }
      SCardEndTransaction(state->card, SCARD_LEAVE_CARD);
      return YKHSMAUTHR_MEMORY_ERROR;
    }

    // Issue GET_RESPONSE (0x00 0xC0 0x00 0x00 LE)
    get_response_apdu[4] = le_byte;

    if (state->verbose > 1) {
      fprintf(stderr, "> ");
      dump_hex(get_response_apdu, sizeof(get_response_apdu));
      fprintf(stderr, "\n");
    }

    // Prepare buffer for next chunk (excluding the SW bytes from previous response)
    recv_ptr = data + total_recv_len - 2; // Overwrite SW bytes of previous chunk
    temp_recv_len = *recv_len - (total_recv_len - 2);

    rc = SCardTransmit(state->card, SCARD_PCI_T1, get_response_apdu,
                        sizeof(get_response_apdu), NULL, recv_ptr, &temp_recv_len);

    if (rc != SCARD_S_SUCCESS) {
      if (state->verbose) {
        fprintf(stderr, "SCardTransmit failed: rc=%08x\n", (unsigned int) rc);
      }
      SCardEndTransaction(state->card, SCARD_LEAVE_CARD);
      return YKHSMAUTHR_PCSC_ERROR;
    }

    if (temp_recv_len < 2) {
      if (state->verbose) {
        fprintf(stderr, "Response too short: %lu bytes\n", (unsigned long) temp_recv_len);
      }
      SCardEndTransaction(state->card, SCARD_LEAVE_CARD);
      return YKHSMAUTHR_GENERIC_ERROR;
    }

    *sw = (recv_ptr[temp_recv_len - 2] << 8) | recv_ptr[temp_recv_len - 1];
    if (temp_recv_len == 2 && (*sw & 0xFF00) == 0x6100) {
      if (state->verbose) {
        fprintf(stderr, "Chained response made no progress\n");
      }
      SCardEndTransaction(state->card, SCARD_LEAVE_CARD);
      return YKHSMAUTHR_GENERIC_ERROR;
    }
    total_recv_len = total_recv_len - 2 + temp_recv_len;

    if (state->verbose > 1) {
      fprintf(stderr, "< ");
      dump_hex(recv_ptr, temp_recv_len);
      fprintf(stderr, "\n");
      fprintf(stderr, "SW: %04x\n", *sw);
    }
  }

  SCardEndTransaction(state->card, SCARD_LEAVE_CARD);

  // Update the final receive length (without the 2-byte SW)
  *recv_len = total_recv_len - 2;

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_connect(ykhsmauth_state *state, const char *wanted) {

  if (state == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  char readers[2048] = {0};
  size_t readers_len = sizeof(readers);
  ykhsmauth_rc ret = ykhsmauth_list_readers(state, readers, &readers_len);
  if (ret != YKHSMAUTHR_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to list_readers: %s\n", ykhsmauth_strerror(ret));
    }

    return ret;
  }
  for (char *reader_ptr = readers; *reader_ptr != '\0';
       reader_ptr += strlen(reader_ptr) + 1) {
    if (wanted) {
      bool found = false;
      char *ptr = reader_ptr;
      while (strlen(ptr) >= strlen(wanted)) {
        if (strncasecmp(ptr, wanted, strlen(wanted)) == 0) {
          found = true;
          break;
        }
        ptr++;
      }
      if (found == false) {
        if (state->verbose) {
          fprintf(stderr, "Skipping reader '%s' since it doesn't match '%s'\n",
                  reader_ptr, wanted);
        }
        continue;
      }
    }

    if (state->verbose) {
      fprintf(stderr, "Trying to connect to reader '%s'\n", reader_ptr);
    }

    DWORD active_protocol = 0;
    int32_t rc =
      SCardConnect(state->context, reader_ptr, SCARD_SHARE_SHARED,
                   SCARD_PROTOCOL_T1, &state->card, &active_protocol);

    if (rc != SCARD_S_SUCCESS) {
      if (state->verbose) {
        fprintf(stderr, "SCardConnect failed, rc=%08x\n", rc);
      }
      continue;
    }

    APDU apdu = {
      {0, 0xa4, 0x04, 0, 8, {0xa0, 0x00, 0x00, 0x05, 0x27, 0x21, 0x07, 0x01}}};
    unsigned char data[256] = {0};
    DWORD recv_len = sizeof(data);
    uint16_t sw = 0;

    if ((ret = send_data(state, &apdu, data, &recv_len, &sw)) !=
        YKHSMAUTHR_SUCCESS) {
      if (state->verbose) {
        fprintf(stderr, "Failed communicating with card: '%s'\n",
                ykhsmauth_strerror(ret));
      }
    } else if (sw != SW_SUCCESS) {
      if (state->verbose) {
        fprintf(stderr, "Failed selecting application: %04x\n", sw);
      }
    } else {
      return YKHSMAUTHR_SUCCESS;
    }

    if (state->verbose) {
      fprintf(stderr, "Disconnecting reader '%s'\n", reader_ptr);
    }
    SCardDisconnect(state->card, SCARD_LEAVE_CARD);
    state->card = 0;
  }

  if (state->verbose) {
    fprintf(stderr, "No usable reader found\n");
  }

  return YKHSMAUTHR_GENERIC_ERROR;
}

ykhsmauth_rc ykhsmauth_list_readers(ykhsmauth_state *state, char *readers,
                                    size_t *len) {
  if (state == NULL || readers == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  if (SCardIsValidContext(state->context) != SCARD_S_SUCCESS) {
    state->card = 0;
    int32_t rc =
      SCardEstablishContext(SCARD_SCOPE_SYSTEM, NULL, NULL, &state->context);
    if (rc != SCARD_S_SUCCESS) {
      if (state->verbose) {
        fprintf(stderr, "SCardEstablishContext failed, rc=%08x\n", rc);
      }
      return YKHSMAUTHR_PCSC_ERROR;
    }
  }

  DWORD readers_len = *len;
  int32_t rc = SCardListReaders(state->context, NULL, readers, &readers_len);
  if (rc != SCARD_S_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "SCardListReaders failed rc=%08x\n", rc);
    }
    return YKHSMAUTHR_PCSC_ERROR;
  }

  *len = readers_len;

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_get_version_ex(ykhsmauth_state *state, uint8_t *major,
                                      uint8_t *minor, uint8_t *patch) {
  if (state == NULL || major == NULL || minor == NULL || patch == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {{0, YKHSMAUTH_INS_GET_VERSION, 0, 0, 0, {0}}};
  unsigned char data[256] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;
  ykhsmauth_rc res;
  if ((res = send_data(state, &apdu, data, &recv_len, &sw)) !=
      YKHSMAUTHR_SUCCESS) {
    return res;
  } else if (sw == SW_SUCCESS && recv_len == 3) {
    *major = data[0];
    *minor = data[1];
    *patch = data[2];
    return YKHSMAUTHR_SUCCESS;
  } else {
    return translate_error(sw, NULL);
  }
}

ykhsmauth_rc ykhsmauth_get_version(ykhsmauth_state *state, char *version,
                                   size_t len) {
  if (version == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  uint8_t v[3] = {0};
  ykhsmauth_rc res = ykhsmauth_get_version_ex(state, &v[0], &v[1], &v[2]);
  if(res == YKHSMAUTHR_SUCCESS) {
    int result = snprintf(version, len, "%d.%d.%d", v[0], v[1], v[2]);
    if (result < 0) {
      if (state->verbose) {
        fprintf(stderr, "Version buffer too small\n");
      }
      return YKHSMAUTHR_GENERIC_ERROR;
    }
  }
  return res;
}

ykhsmauth_rc ykhsmauth_put(ykhsmauth_state *state, const uint8_t *mgmkey,
                           size_t mgmkey_len, const char *label, uint8_t algo,
                           const uint8_t *key, size_t key_len,
                           const uint8_t *cpw, size_t cpw_len,
                           const uint8_t touch_policy, uint8_t *retries) {
  if (state == NULL || mgmkey == NULL || mgmkey_len != YKHSMAUTH_PW_LEN ||
      label == NULL || strlen(label) < YKHSMAUTH_MIN_LABEL_LEN ||
      strlen(label) > YKHSMAUTH_MAX_LABEL_LEN || key == NULL ||
      key_len > YKHSMAUTH_YUBICO_ECP256_PRIVKEY_LEN || cpw == NULL ||
      cpw_len > YKHSMAUTH_PW_LEN) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {{0, YKHSMAUTH_INS_PUT, 0, 0, 0, {0}}};

  add_tag(&apdu, YKHSMAUTH_TAG_MGMKEY, mgmkey, mgmkey_len, 0);
  add_tag(&apdu, YKHSMAUTH_TAG_LABEL, label, strlen(label), 0);
  add_tag(&apdu, YKHSMAUTH_TAG_ALGO, &algo, sizeof(algo), 0);

  if (algo == YKHSMAUTH_YUBICO_AES128_ALGO) {
    add_tag(&apdu, YKHSMAUTH_TAG_KEY_ENC, key, key_len / 2, 0);
    add_tag(&apdu, YKHSMAUTH_TAG_KEY_MAC, key + key_len / 2, key_len / 2, 0);
  } else if (algo == YKHSMAUTH_YUBICO_ECP256_ALGO) {
    add_tag(&apdu, YKHSMAUTH_TAG_PRIVKEY, key, key_len, 0);
  }

  add_tag(&apdu, YKHSMAUTH_TAG_PW, cpw, cpw_len, YKHSMAUTH_PW_LEN - cpw_len);
  add_tag(&apdu, YKHSMAUTH_TAG_TOUCH, &touch_policy, sizeof(touch_policy), 0);

  unsigned char data[256] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;

  ykhsmauth_rc rc = send_data(state, &apdu, data, &recv_len, &sw);
  if (rc != YKHSMAUTHR_SUCCESS) {
    return rc;
  } else if (sw != SW_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to store credential: %04x\n", sw);
    }

    return translate_error(sw, retries);
  }

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_delete(ykhsmauth_state *state, uint8_t *mgmkey,
                              size_t mgmkey_len, char *label,
                              uint8_t *retries) {
  if (state == NULL || mgmkey == NULL || mgmkey_len != YKHSMAUTH_PW_LEN ||
      label == NULL || strlen(label) < YKHSMAUTH_MIN_LABEL_LEN ||
      strlen(label) > YKHSMAUTH_MAX_LABEL_LEN) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {{0, YKHSMAUTH_INS_DELETE, 0, 0, 0, {0}}};

  add_tag(&apdu, YKHSMAUTH_TAG_MGMKEY, mgmkey, mgmkey_len, 0);
  add_tag(&apdu, YKHSMAUTH_TAG_LABEL, label, strlen(label), 0);

  unsigned char data[256] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;

  ykhsmauth_rc rc = send_data(state, &apdu, data, &recv_len, &sw);
  if (rc != YKHSMAUTHR_SUCCESS) {
    return rc;
  } else if (sw != SW_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to delete credential: %04x\n", sw);
    }

    return translate_error(sw, retries);
  }

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_calculate(ykhsmauth_state *state, const char *label,
                                 uint8_t *context, size_t context_len,
                                 const uint8_t *pw, size_t pw_len,
                                 uint8_t *key_s_enc, size_t key_s_enc_len,
                                 uint8_t *key_s_mac, size_t key_s_mac_len,
                                 uint8_t *key_s_rmac, size_t key_s_rmac_len,
                                 uint8_t *retries) {
  return ykhsmauth_calculate_ex(state, label, context, context_len, NULL, 0,
                                NULL, 0, pw, pw_len, key_s_enc, key_s_enc_len,
                                key_s_mac, key_s_mac_len, key_s_rmac,
                                key_s_rmac_len, retries);
}

ykhsmauth_rc ykhsmauth_calculate_ex(
  ykhsmauth_state *state, const char *label, uint8_t *context,
  size_t context_len, uint8_t *card_pubkey, size_t card_pubkey_len,
  uint8_t *card_crypto, size_t card_crypto_len, const uint8_t *pw,
  size_t pw_len, uint8_t *key_s_enc, size_t key_s_enc_len, uint8_t *key_s_mac,
  size_t key_s_mac_len, uint8_t *key_s_rmac, size_t key_s_rmac_len,
  uint8_t *retries) {

  if (state == NULL || label == NULL ||
      strlen(label) < YKHSMAUTH_MIN_LABEL_LEN ||
      strlen(label) > YKHSMAUTH_MAX_LABEL_LEN || context == NULL ||
      context_len > 2 * YKHSMAUTH_YUBICO_ECP256_PUBKEY_LEN ||
      card_pubkey_len > YKHSMAUTH_YUBICO_ECP256_PUBKEY_LEN ||
      card_crypto_len > YKHSMAUTH_SESSION_KEY_LEN || pw == NULL ||
      pw_len > YKHSMAUTH_PW_LEN || key_s_enc == NULL ||
      key_s_enc_len != YKHSMAUTH_SESSION_KEY_LEN || key_s_mac == NULL ||
      key_s_mac_len != YKHSMAUTH_SESSION_KEY_LEN || key_s_rmac == NULL ||
      key_s_rmac_len != YKHSMAUTH_SESSION_KEY_LEN) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {{0, YKHSMAUTH_INS_CALCULATE, 0, 0, 0, {0}}};

  add_tag(&apdu, YKHSMAUTH_TAG_LABEL, label, strlen(label), 0);
  add_tag(&apdu, YKHSMAUTH_TAG_CONTEXT, context, context_len, 0);

  if (card_pubkey && card_pubkey_len) {
    add_tag(&apdu, YKHSMAUTH_TAG_PUBKEY, card_pubkey, card_pubkey_len, 0);
  }

  // Only send card_crypto for asym auth
  if (card_crypto && card_crypto_len > YKHSMAUTH_CARD_CRYPTO_LEN) {
    add_tag(&apdu, YKHSMAUTH_TAG_RESPONSE, card_crypto, card_crypto_len, 0);
  }

  add_tag(&apdu, YKHSMAUTH_TAG_PW, pw, pw_len, YKHSMAUTH_PW_LEN - pw_len);

  unsigned char data[256] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;

  ykhsmauth_rc rc = send_data(state, &apdu, data, &recv_len, &sw);
  if (rc != YKHSMAUTHR_SUCCESS) {
    return rc;
  } else if (sw != SW_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to derive keys: %04x\n", sw);
    }

    return translate_error(sw, retries);
  }

  if (recv_len != 3 * YKHSMAUTH_SESSION_KEY_LEN) {
    if (state->verbose) {
      fprintf(stderr, "Wrong length returned: %zu\n", (size_t) recv_len);
    }
    return YKHSMAUTHR_GENERIC_ERROR;
  }

  uint8_t *ptr = data;

  memcpy(key_s_enc, ptr, YKHSMAUTH_SESSION_KEY_LEN);
  ptr += YKHSMAUTH_SESSION_KEY_LEN;
  memcpy(key_s_mac, ptr, YKHSMAUTH_SESSION_KEY_LEN);
  ptr += YKHSMAUTH_SESSION_KEY_LEN;
  memcpy(key_s_rmac, ptr, YKHSMAUTH_SESSION_KEY_LEN);
  ptr += YKHSMAUTH_SESSION_KEY_LEN;

  // Ignore host crypto for now

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_reset(ykhsmauth_state *state) {

  if (state == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {
    {0, YKHSMAUTH_INS_RESET, YKHSMAUTH_P1_RESET, YKHSMAUTH_P2_RESET, 0, {0}}};
  unsigned char data[256] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;

  ykhsmauth_rc res = send_data(state, &apdu, data, &recv_len, &sw);
  if (sw != SW_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to reset: %s\n", ykhsmauth_strerror(res));
    }

    return translate_error(sw, NULL);
  }

  return res;
}

ykhsmauth_rc ykhsmauth_list_keys(ykhsmauth_state *state,
                                 ykhsmauth_list_entry *list,
                                 size_t *list_items) {

  if (state == NULL || list_items == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {{0, YKHSMAUTH_INS_LIST, 0, 0, 0, {0}}};
  unsigned char data[3072] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;

  ykhsmauth_rc rc = send_data(state, &apdu, data, &recv_len, &sw);
  if (rc != YKHSMAUTHR_SUCCESS) {
    return rc;
  } else if (sw != SW_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to list keys: %04x\n", sw);
    }

    return translate_error(sw, NULL);
  }

  size_t element = 0;
  size_t i = 0;

  // i + 1 here guarantees we can read tag and len
  while (i + 1 < recv_len) {
    if (data[i++] == YKHSMAUTH_TAG_LABEL_LIST) {
      size_t len = data[i++];
      if (list != NULL) {
        if (element >= *list_items) {
          return YKHSMAUTHR_MEMORY_ERROR;
        } else if (i + len > recv_len || len < 3 ||
                   len - 3 > sizeof(list[element].label)) {
          if (state->verbose) {
            fprintf(stderr,
                    "Length of element doesn't match expectations (%zu)\n",
                    len);
          }
          return YKHSMAUTHR_GENERIC_ERROR;
        }

        list[element].algo = data[i++];
        list[element].touch = data[i++];
        memset(list[element].label, 0, sizeof(list[element].label));
        memcpy(list[element].label, data + i, len - 3);
        i += len - 3;
        list[element].ctr = data[i++];
      } else {
        i += len;
      }
      element++;
    } else {
      if (state->verbose) {
        fprintf(stderr, "Unexpected tag returned on list\n");
      }
      return YKHSMAUTHR_GENERIC_ERROR;
    }
  }

  *list_items = element;

  if (i != recv_len) {
    return YKHSMAUTHR_GENERIC_ERROR;
  }

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_get_challenge(ykhsmauth_state *state, const char *label,
                                     uint8_t *challenge,
                                     size_t *challenge_len) {
  return ykhsmauth_get_challenge_ex(state, label, NULL, 0, challenge, challenge_len);
}

ykhsmauth_rc ykhsmauth_get_challenge_ex(ykhsmauth_state *state, const char *label,
                                     const uint8_t *cpw, size_t cpw_len,
                                     uint8_t *challenge,
                                     size_t *challenge_len) {

  if (state == NULL || label == NULL || cpw_len > YKHSMAUTH_PW_LEN ||
      strlen(label) < YKHSMAUTH_MIN_LABEL_LEN ||
      strlen(label) > YKHSMAUTH_MAX_LABEL_LEN || challenge == NULL ||
      challenge_len == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {{0, YKHSMAUTH_INS_GET_CHALLENGE, 0, 0, 0, {0}}};

  add_tag(&apdu, YKHSMAUTH_TAG_LABEL, label, strlen(label), 0);
  if(cpw && cpw_len) {
    add_tag(&apdu, YKHSMAUTH_TAG_PW, cpw, cpw_len, YKHSMAUTH_PW_LEN - cpw_len);
  }

  unsigned char data[256] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;

  ykhsmauth_rc rc = send_data(state, &apdu, data, &recv_len, &sw);
  if (rc != YKHSMAUTHR_SUCCESS) {
    return rc;
  } else if (sw != SW_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to get challenge: %04x\n", sw);
    }

    return translate_error(sw, NULL);
  }

  if (*challenge_len < recv_len) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  *challenge_len = recv_len;
  memcpy(challenge, data, recv_len);

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_get_pubkey(ykhsmauth_state *state, const char *label,
                                  uint8_t *pubkey, size_t *pubkey_len) {

  if (state == NULL || label == NULL ||
      strlen(label) < YKHSMAUTH_MIN_LABEL_LEN ||
      strlen(label) > YKHSMAUTH_MAX_LABEL_LEN || pubkey == NULL ||
      pubkey_len == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {{0, YKHSMAUTH_INS_GET_PUBKEY, 0, 0, 0, {0}}};

  add_tag(&apdu, YKHSMAUTH_TAG_LABEL, label, strlen(label), 0);

  unsigned char data[256] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;

  ykhsmauth_rc rc = send_data(state, &apdu, data, &recv_len, &sw);
  if (rc != YKHSMAUTHR_SUCCESS) {
    return rc;
  } else if (sw != SW_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to get pubkey: %04x\n", sw);
    }

    return translate_error(sw, NULL);
  }

  if (*pubkey_len < recv_len) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  *pubkey_len = recv_len;
  memcpy(pubkey, data, recv_len);

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_get_mgmkey_retries(ykhsmauth_state *state,
                                          uint8_t *retries) {
  if (state == NULL || retries == NULL) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {{0, YKHSMAUTH_INS_GET_MGMKEY_RETRIES, 0, 0, 0, {0}}};
  unsigned char data[256] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;

  ykhsmauth_rc rc = send_data(state, &apdu, data, &recv_len, &sw);
  if (rc != YKHSMAUTHR_SUCCESS) {
    return rc;
  } else if (sw != SW_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to get Management key retries: %04x\n", sw);
    }

    return translate_error(sw, NULL);
  }

  *retries = data[0];

  return YKHSMAUTHR_SUCCESS;
}

ykhsmauth_rc ykhsmauth_put_mgmkey(ykhsmauth_state *state, uint8_t *mgmkey,
                                  size_t mgmkey_len, uint8_t *new_mgmkey,
                                  size_t new_mgmkey_len, uint8_t *retries) {
  if (state == NULL || mgmkey == NULL || mgmkey_len != YKHSMAUTH_PW_LEN ||
      new_mgmkey == NULL || new_mgmkey_len != YKHSMAUTH_PW_LEN) {
    return YKHSMAUTHR_INVALID_PARAMS;
  }

  APDU apdu = {{0, YKHSMAUTH_INS_PUT_MGMKEY, 0, 0, 0, {0}}};

  add_tag(&apdu, YKHSMAUTH_TAG_MGMKEY, mgmkey, mgmkey_len, 0);
  add_tag(&apdu, YKHSMAUTH_TAG_MGMKEY, new_mgmkey, new_mgmkey_len, 0);

  unsigned char data[256] = {0};
  DWORD recv_len = sizeof(data);
  uint16_t sw = 0;

  ykhsmauth_rc rc = send_data(state, &apdu, data, &recv_len, &sw);
  if (rc != YKHSMAUTHR_SUCCESS) {
    return rc;
  } else if (sw != SW_SUCCESS) {
    if (state->verbose) {
      fprintf(stderr, "Unable to store Management key: %04x\n", sw);
    }

    return translate_error(sw, retries);
  }

  return YKHSMAUTHR_SUCCESS;
}
