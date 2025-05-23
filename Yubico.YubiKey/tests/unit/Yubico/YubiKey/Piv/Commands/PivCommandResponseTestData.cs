// Copyright 2021 Yubico AB
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Security.Cryptography;
using Yubico.Core.Tlv;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.TestUtilities;

namespace Yubico.YubiKey.Piv.Commands
{
    public static class PivCommandResponseTestData
    {
        public static byte[] GetEncryptedBlock(KeyType keyType) => keyType switch
        {
            KeyType.RSA3072 => new byte[] //384 bytes
            {
                0x10, 0xa0, 0x49, 0xc0, 0xa1, 0x5e, 0x18, 0xa1, 0x0d, 0xbb, 0x92, 0xd9, 0x34, 0xae, 0xb3, 0x2a,
                0x5b, 0x91, 0x5d, 0x8a, 0xe7, 0xaf, 0xe6, 0x11, 0xa3, 0x3e, 0x18, 0xdb, 0xdc, 0x51, 0x74, 0xd2,
                0x9c, 0xb9, 0xa7, 0x4c, 0xee, 0x22, 0x23, 0xb3, 0xd0, 0x1b, 0xe8, 0x9d, 0x88, 0xfc, 0x09, 0x9c,
                0x3a, 0xe9, 0x96, 0x2b, 0x6e, 0x11, 0x1e, 0x20, 0x5f, 0xa0, 0x36, 0x4d, 0x31, 0xa8, 0xb0, 0xf8,
                0x9d, 0x42, 0x04, 0xf0, 0x9a, 0xd4, 0x36, 0x5e, 0xb9, 0x9c, 0x51, 0x84, 0x63, 0x30, 0xb5, 0x02,
                0xd8, 0xd3, 0xab, 0x38, 0x06, 0x33, 0xd3, 0xd0, 0x89, 0x2e, 0xa6, 0xfe, 0x83, 0xaf, 0x2b, 0x93,
                0x61, 0xdb, 0x22, 0xbe, 0x0a, 0xcb, 0xbe, 0x7e, 0xf0, 0xdf, 0x5e, 0x62, 0xc8, 0xd1, 0x82, 0x99,
                0x98, 0xca, 0x11, 0xf1, 0x8f, 0xcb, 0x5b, 0x4b, 0x4e, 0xca, 0x28, 0x14, 0xca, 0x01, 0x5d, 0x8b,
                0x1c, 0x69, 0xb3, 0x1e, 0xfe, 0xab, 0x5c, 0x83, 0x3b, 0x80, 0x8f, 0x50, 0x3d, 0xce, 0xfa, 0x59,
                0x1c, 0x28, 0x3c, 0x7f, 0x6f, 0x08, 0xa9, 0x4e, 0x5f, 0x58, 0x6b, 0x94, 0x74, 0x26, 0xbf, 0x73,
                0x69, 0x45, 0x87, 0x95, 0xfb, 0x1f, 0x9b, 0x22, 0x7a, 0xb9, 0xe5, 0x8c, 0x3b, 0xf6, 0xf6, 0x58,
                0xb7, 0x71, 0x76, 0xad, 0x3e, 0x99, 0x7b, 0x16, 0x2b, 0x1b, 0xe4, 0x17, 0x09, 0x35, 0x89, 0x26,
                0x42, 0x64, 0xd0, 0x63, 0xdb, 0x2c, 0xb4, 0x7b, 0xfc, 0x06, 0x3d, 0xe3, 0xbb, 0x50, 0x23, 0xa2,
                0x00, 0x22, 0x85, 0x0e, 0x17, 0xb3, 0x6b, 0x0f, 0x60, 0x8d, 0xc2, 0x60, 0xcc, 0x42, 0xb7, 0x40,
                0x7a, 0x29, 0x34, 0x22, 0xc6, 0x07, 0x86, 0xdd, 0x51, 0x4c, 0x63, 0x5d, 0x30, 0xaf, 0xe3, 0xa4,
                0xa4, 0xc4, 0xf6, 0x42, 0xb2, 0x05, 0x22, 0xc2, 0x5e, 0x62, 0xff, 0x34, 0x70, 0xd1, 0x82, 0x82,
                0x67, 0x9b, 0xf5, 0x50, 0x81, 0x04, 0xbd, 0x83, 0xf4, 0xe2, 0xa7, 0x18, 0x5f, 0xda, 0x6c, 0x5e,
                0x15, 0x0f, 0x15, 0x70, 0x1e, 0x09, 0x5c, 0x00, 0xfa, 0x94, 0xc3, 0x23, 0x89, 0x02, 0xc2, 0x8b,
                0xfa, 0xb7, 0x29, 0x88, 0x3a, 0xa6, 0xc5, 0xd0, 0x31, 0x03, 0x95, 0x38, 0xbd, 0xa1, 0xef, 0x51,
                0x08, 0xee, 0xc5, 0xa0, 0x36, 0x7a, 0xa2, 0xa1, 0x27, 0xc2, 0xb3, 0xbc, 0xe1, 0xcd, 0x42, 0x55,
                0xbb, 0x7f, 0xb9, 0x9d, 0xff, 0xab, 0x76, 0x49, 0x0a, 0xfd, 0xbe, 0x75, 0x31, 0x66, 0x7d, 0xca,
                0x02, 0x58, 0xd3, 0x6c, 0x44, 0x09, 0x3d, 0x1d, 0x8e, 0x5c, 0x97, 0x22, 0x23, 0x3a, 0xda, 0xe9,
                0x43, 0x81, 0xae, 0xf0, 0x70, 0x5c, 0xa2, 0xda, 0x85, 0x8d, 0xe1, 0x80, 0x5a, 0xc1, 0x82, 0x84,
                0x41, 0xcd, 0x8d, 0x62, 0x0a, 0x00, 0x81, 0x94, 0xad, 0x55, 0xee, 0xf2, 0x37, 0x67, 0x08, 0x90
            },
            KeyType.RSA2048 => new byte[] //256 bytes
            {
                0x7F, 0x71, 0xDA, 0x5B, 0x16, 0xAA, 0x7D, 0x15, 0x50, 0x8A, 0x6A, 0x57, 0x3C, 0x78, 0x86, 0xBB,
                0xF7, 0x53, 0x29, 0xE0, 0xC4, 0x9C, 0xF8, 0xC8, 0xD5, 0x37, 0xD4, 0xD4, 0xE5, 0x3F, 0x9D, 0xDE,
                0x11, 0x17, 0xB4, 0x11, 0xEE, 0x45, 0xD4, 0x1E, 0xB9, 0x75, 0x92, 0x55, 0x34, 0xE6, 0x2B, 0x1F,
                0x8A, 0x49, 0x20, 0x48, 0xAD, 0xE4, 0xD0, 0xF4, 0x2C, 0xDC, 0xF5, 0x80, 0xB7, 0x25, 0x49, 0x83,
                0xB3, 0x43, 0x14, 0x0F, 0x31, 0xE7, 0xE1, 0xF0, 0xB4, 0xF8, 0x75, 0xC1, 0xB7, 0x9E, 0xF9, 0x6A,
                0x2D, 0xBC, 0x3A, 0xF8, 0x2F, 0x84, 0x4D, 0xFC, 0x42, 0x27, 0x21, 0xF1, 0x23, 0x13, 0x50, 0xEA,
                0x96, 0x05, 0x47, 0x7C, 0xBF, 0x0C, 0x97, 0x46, 0x6B, 0x1D, 0xA6, 0x5F, 0x80, 0xB9, 0x7B, 0x89,
                0x8A, 0xF4, 0x8C, 0xC3, 0x4B, 0x9F, 0xAB, 0x91, 0x29, 0xBB, 0xC3, 0x70, 0x7A, 0x9C, 0x99, 0xE6,
                0x48, 0x33, 0x90, 0xB5, 0x49, 0x97, 0xAD, 0xD0, 0x6B, 0x0B, 0x36, 0x10, 0xA9, 0xB2, 0xFC, 0xCA,
                0xD7, 0x8C, 0xEC, 0x30, 0x6D, 0x50, 0xCB, 0xBE, 0x57, 0xD7, 0x63, 0x3E, 0xC1, 0xA9, 0x80, 0x7F,
                0xE6, 0x37, 0xFA, 0x51, 0xD8, 0x8C, 0x0B, 0x22, 0x70, 0x95, 0x1B, 0x7A, 0xEA, 0x5C, 0xE3, 0x43,
                0xD7, 0x09, 0x77, 0x54, 0xC4, 0x39, 0x40, 0xF5, 0xB1, 0xA5, 0xBE, 0xD7, 0x0C, 0x96, 0xFC, 0x74,
                0x41, 0x93, 0x4A, 0x27, 0xC7, 0x07, 0xCE, 0x2F, 0x3A, 0xFD, 0xC1, 0xFB, 0xF8, 0xD3, 0x06, 0xB9,
                0x02, 0xD0, 0x16, 0xC7, 0x21, 0x46, 0x38, 0x74, 0x2F, 0x50, 0x1E, 0xCF, 0x95, 0xA9, 0xB6, 0x39,
                0x74, 0xAC, 0x15, 0x7B, 0xE8, 0x23, 0x81, 0x53, 0xF0, 0xAC, 0x15, 0x9F, 0x12, 0xDE, 0x6C, 0xDB,
                0xC5, 0xF2, 0xF0, 0x01, 0x7E, 0x42, 0x31, 0x0E, 0x67, 0x13, 0x97, 0x38, 0x84, 0xCD, 0xA5, 0xD1
            },

            _ => new byte[]
            {
                0x27, 0xF2, 0x22, 0xE8, 0x5C, 0xF3, 0xDB, 0x24, 0x51, 0x93, 0xC5, 0xED, 0x30, 0x96, 0x20, 0x4D,
                0xC5, 0xCD, 0x5B, 0x80, 0x0D, 0x9A, 0xBE, 0x1F, 0x1C, 0x57, 0x80, 0x83, 0xDA, 0x2E, 0x0A, 0x60,
                0xAD, 0x0E, 0xA2, 0x29, 0x9C, 0xD5, 0x82, 0x1A, 0x8C, 0x03, 0x4D, 0x87, 0x72, 0x66, 0x59, 0x94,
                0x85, 0x83, 0x82, 0x8E, 0xD4, 0x0A, 0xC8, 0xF4, 0x63, 0xB8, 0x09, 0xFF, 0x77, 0xD0, 0xE7, 0x5D,
                0xD3, 0x8F, 0x39, 0xCF, 0x24, 0x39, 0x67, 0x3A, 0xD8, 0xCB, 0x44, 0xE7, 0xB4, 0x7F, 0x3D, 0xD4,
                0x68, 0xE8, 0x6B, 0x83, 0x65, 0xA7, 0x2B, 0x8C, 0xFE, 0x36, 0x9D, 0xE1, 0x15, 0x94, 0x26, 0xA0,
                0x6F, 0x3D, 0xBC, 0x4B, 0x97, 0x16, 0x5E, 0x07, 0x89, 0xF3, 0x9D, 0xB4, 0xBC, 0x84, 0x4B, 0xE9,
                0xAF, 0xEF, 0xE8, 0xD1, 0x08, 0x08, 0x21, 0x56, 0x35, 0xAD, 0xB5, 0xD3, 0x31, 0x53, 0x20, 0x9B
            },
        };

        // Get a sample digest for the given keyType.
        // If RSA, this gets a PKCS1 v1.5 formatted block.
        // If ECC P256, it returns 32 bytes.
        // If ECC P384, it returns 48 bytes.
        public static byte[] GetDigestData(KeyType keyType) => keyType switch
        {
            KeyType.RSA2048 => new byte[]
            {
                0x00, 0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x30,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
            },

            KeyType.RSA3072 => new byte[]
            {
                0x00, 0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x30,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
            },

            KeyType.RSA4096 => new byte[]
            {
                0x00, 0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x30,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x00, 0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x30,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
            },

            KeyType.ECP256 => new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
            },

            KeyType.ECP384 => new byte[]
            {
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f
            },

            _ => new byte[]
            {
                0x00, 0x01, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x30,
                0x2f, 0x30, 0x0b, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x04, 0x20,
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f
            },
        };

        public static List<byte> GetDataCommandExpectedApduData(PivDataTag tag) => tag switch
        {
            PivDataTag.Chuid => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x02 }),
            PivDataTag.Capability => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x07 }),
            PivDataTag.Discovery => new List<byte>(new byte[] { 0x5C, 0x01, 0x7E }),
            PivDataTag.Authentication => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x05 }),
            PivDataTag.Signature => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x0A }),
            PivDataTag.KeyManagement => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x0B }),
            PivDataTag.CardAuthentication => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x01 }),
            PivDataTag.Retired1 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x0D }),
            PivDataTag.Retired2 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x0E }),
            PivDataTag.Retired3 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x0F }),
            PivDataTag.Retired4 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x10 }),
            PivDataTag.Retired5 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x11 }),
            PivDataTag.Retired6 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x12 }),
            PivDataTag.Retired7 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x13 }),
            PivDataTag.Retired8 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x14 }),
            PivDataTag.Retired9 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x15 }),
            PivDataTag.Retired10 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x16 }),
            PivDataTag.Retired11 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x17 }),
            PivDataTag.Retired12 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x18 }),
            PivDataTag.Retired13 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x19 }),
            PivDataTag.Retired14 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x1A }),
            PivDataTag.Retired15 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x1B }),
            PivDataTag.Retired16 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x1C }),
            PivDataTag.Retired17 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x1D }),
            PivDataTag.Retired18 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x1E }),
            PivDataTag.Retired19 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x1F }),
            PivDataTag.Retired20 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x20 }),
            PivDataTag.Printed => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x09 }),
            PivDataTag.SecurityObject => new List<byte>(new byte[] { 0x5c, 0x03, 0x5F, 0xC1, 0x06 }),
            PivDataTag.KeyHistory => new List<byte>(new byte[] { 0x5c, 0x03, 0x5F, 0xC1, 0x0C }),
            PivDataTag.IrisImages => new List<byte>(new byte[] { 0x5c, 0x03, 0x5F, 0xC1, 0x21 }),
            PivDataTag.FacialImage => new List<byte>(new byte[] { 0x5c, 0x03, 0x5F, 0xC1, 0x08 }),
            PivDataTag.Fingerprints => new List<byte>(new byte[] { 0x5c, 0x03, 0x5F, 0xC1, 0x03 }),
            PivDataTag.BiometricGroupTemplate => new List<byte>(new byte[] { 0x5c, 0x02, 0x7F, 0x61 }),
            PivDataTag.SecureMessageSigner => new List<byte>(new byte[] { 0x5c, 0x03, 0x5F, 0xC1, 0x22 }),
            PivDataTag.PairingCodeReferenceData => new List<byte>(new byte[] { 0x5c, 0x03, 0x5F, 0xC1, 0x23 }),
            _ => new List<byte>(),
        };

        public static List<byte> GetDataCommandExpectedApduDataInt(int tag) => tag switch
        {
            0x0000007E => new List<byte>(new byte[] { 0x5C, 0x01, 0x7E }),
            0x00007F61 => new List<byte>(new byte[] { 0x5C, 0x02, 0x7F, 0x61 }),
            0x005FC109 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xC1, 0x09 }),
            0x005FFF01 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x01 }),
            0x005FFF00 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x00 }),
            0x005FFF10 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x10 }),
            0x005FFF11 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x11 }),
            0x005FFF12 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x12 }),
            0x005FFF13 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x13 }),
            0x005FFF14 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x14 }),
            0x005FFF15 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0x15 }),
            0x005FFFFF => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0xFF, 0xFF }),
            0x005F0000 => new List<byte>(new byte[] { 0x5C, 0x03, 0x5F, 0x00, 0x00 }),
            _ => new List<byte>(),
        };

        public static byte[] PutDataEncoding(PivDataTag tag, bool isCorrect)
        {
            int[] format;
            int indexRandom = 1;
            switch (tag)
            {
                case PivDataTag.Chuid:
                    format = new int[]
                    {
                        0x53,
                        0x30, 0, 25,
                        0x34, 0, 16,
                        0x35, 0, 8,
                        0x3E, 0, 0,
                        0xFE, 0, 0
                    };
                    break;

                case PivDataTag.Capability:
                    format = new int[]
                    {
                        0x53,
                        0xF0, 0, 21,
                        0xF1, 0, 1,
                        0xF2, 0, 1,
                        0xF3, 0, 0,
                        0xF4, 0, 1,
                        0xF5, 0, 1,
                        0xF6, 0, 0,
                        0xF7, 0, 0,
                        0xFA, 0, 0,
                        0xFB, 0, 0,
                        0xFC, 0, 0,
                        0xFD, 0, 0,
                        0xFE, 0, 0
                    };
                    break;

                case PivDataTag.Authentication:
                case PivDataTag.Signature:
                case PivDataTag.KeyManagement:
                case PivDataTag.CardAuthentication:
                case PivDataTag.Retired1:
                case PivDataTag.Retired2:
                case PivDataTag.Retired3:
                case PivDataTag.Retired4:
                case PivDataTag.Retired5:
                case PivDataTag.Retired6:
                case PivDataTag.Retired7:
                case PivDataTag.Retired8:
                case PivDataTag.Retired9:
                case PivDataTag.Retired10:
                case PivDataTag.Retired11:
                case PivDataTag.Retired12:
                case PivDataTag.Retired13:
                case PivDataTag.Retired14:
                case PivDataTag.Retired15:
                case PivDataTag.Retired16:
                case PivDataTag.Retired17:
                case PivDataTag.Retired18:
                case PivDataTag.Retired19:
                case PivDataTag.Retired20:
                    format = new int[]
                    {
                        0x53,
                        0x70, 1856, 0,
                        0x71, 0, 1,
                        0xFE, 0, 0
                    };
                    break;

                case PivDataTag.SecurityObject:
                    format = new int[]
                    {
                        0x53,
                        0xBA, 30, 0,
                        0xBB, 1298, 0,
                        0xFE, 0, 0
                    };
                    break;

                case PivDataTag.KeyHistory:
                    format = new int[]
                    {
                        0x53,
                        0xC1, 0, 1,
                        0xC2, 0, 1,
                        0xF3, 118, 0,
                        0xFE, 0, 0
                    };
                    indexRandom = 7;
                    break;

                case PivDataTag.IrisImages:
                    format = new int[]
                    {
                        0x53,
                        0xBC, 7100, 0,
                        0xFE, 0, 0
                    };
                    break;

                case PivDataTag.FacialImage:
                    format = new int[]
                    {
                        0x53,
                        0xBC, 12704, 0,
                        0xFE, 0, 0
                    };
                    break;

                case PivDataTag.Fingerprints:
                    format = new int[]
                    {
                        0x53,
                        0xBC, 4000, 0,
                        0xFE, 0, 0
                    };
                    break;

                case PivDataTag.SecureMessageSigner:
                    format = new int[]
                    {
                        0x53,
                        0x70, 1856, 0,
                        0x71, 0, 1,
                        0x7F21, 601, 0,
                        0xFE, 0, 0
                    };
                    break;

                case PivDataTag.PairingCodeReferenceData:
                    format = new int[]
                    {
                        0x53,
                        0x99, 0, 8,
                        0xFE, 0, 0
                    };
                    break;

                case PivDataTag.Printed:
                case PivDataTag.Discovery:
                case PivDataTag.BiometricGroupTemplate:
                default:
                    return new byte[] { 0x01, 00 };
            }

            return BuildPutDataEncoding(format, isCorrect, indexRandom);
        }

        private static byte[] BuildPutDataEncoding(int[] format, bool isCorrect, int indexRandom)
        {
            var tlvWriter = new TlvWriter();
            int index = 1;
            using (tlvWriter.WriteNestedTlv(format[0]))
            {
                while (index < format.Length)
                {
                    int valueLen = 1;
                    if (format[index + 1] == 0)
                    {
                        valueLen = format[index + 2];
                        if (isCorrect == false)
                        {
                            valueLen++;
                        }
                    }

                    if (format[index] == 0xFE && isCorrect == false)
                    {
                        valueLen = 1;
                    }

                    if (valueLen == 0)
                    {
                        tlvWriter.WriteValue(format[index], null);
                    }
                    else
                    {
                        byte[] value = new byte[valueLen];
                        if (index == indexRandom)
                        {
                            FillWithRandomBytes(value);
                        }

                        tlvWriter.WriteValue(format[index], value);
                    }

                    index += 3;
                }
            }

            return tlvWriter.Encode();
        }

        private static void FillWithRandomBytes(byte[] buffer)
        {
            using RandomNumberGenerator random = RandomObjectUtility.GetRandomObject(null);
            random.GetBytes(buffer);
        }
    }
}
