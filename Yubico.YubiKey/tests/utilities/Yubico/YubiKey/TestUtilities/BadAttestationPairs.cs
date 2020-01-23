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

using System;
using System.Security.Cryptography.X509Certificates;

namespace Yubico.YubiKey.TestUtilities
{
    public static class BadAttestationPairs
    {
        public const int KeyRsa1024CertValid = 1;
        public const int KeyRsa2048CertVersion1 = 2;
        public const int KeyEccP256CertVersion1 = 3;
        public const int KeyEccP384CertVersion1 = 4;
        public const int KeyEccP256CertEccP384 = 5;
        public const int KeyRsa2048CertDifferent2048 = 6;
        public const int KeyRsa2048CertBigName = 7;

        // Get a Bad pair. There are all kinds of reasons a pair is bad. Use the
        // whichPair arg to specify which kind of bad.
        //   KeyRsa1024CertValid = 1
        //      - RSA 1024, but the cert is valid otherwise
        //   KeyRsa2048CertVersion1 = 2
        //      - RSA 2048, cert is version 1
        //   KeyEccP256CertVersion1 = 3
        //      - ECC P256, cert is version 1
        //   KeyEccP384CertVersion1 = 4
        //      - ECC P384, cert is version 1
        //   KeyEccP256CertEccP384 = 5
        //      - key is ECC P256, cert is valid ECC P384
        //   KeyRsa2048CertDifferent2048 = 6
        //      - key is RSA 2048, cert is valid RSA 2048, but for a different key
        //   KeyRsa2048CertBigName = 7
        //      - key is RSA 2048, cert contains big subject name
        //
        // Note that any whichPair value other than the above will be treated as
        // KeyRsa1024CertValid.
        public static void GetPair(int whichPair, out string privateKeyPem, out string certPem)
        {
            switch (whichPair)
            {
                default:
                case KeyRsa1024CertValid:
                    GetPair1024(out privateKeyPem, out certPem);
                    break;

                case KeyRsa2048CertVersion1:
                    GetPair2048Version1(out privateKeyPem, out certPem);
                    break;

                case KeyEccP256CertVersion1:
                    GetPair256Version1(out privateKeyPem, out certPem);
                    break;

                case KeyEccP384CertVersion1:
                    GetPair384Version1(out privateKeyPem, out certPem);
                    break;

                case KeyEccP256CertEccP384:
                    GetMix256And384(out privateKeyPem, out certPem);
                    break;

                case KeyRsa2048CertDifferent2048:
                    GetMix2048And2048(out privateKeyPem, out certPem);
                    break;

                case KeyRsa2048CertBigName:
                    Get2048BigIssuerName(out privateKeyPem, out certPem);
                    break;
            }
        }

        // Get a pair that is valid, except the key is RSA 1024.
        public static void GetPair1024(out string privateKeyPem, out string certPem)
        {
            privateKeyPem =
                "-----BEGIN PRIVATE KEY-----\n" +
                "MIICdwIBADANBgkqhkiG9w0BAQEFAASCAmEwggJdAgEAAoGBALG/a85wy36uf4Rm\n" +
                "wpI/lSUi4bcueGJYodpUS99AnBeI6HFEK2jBf0niKPXhkHnO/CuajF8yfa67hkHx\n" +
                "C+7O102JIpkp6erHm6pXiSL2CezWt6Zy7Aaj8LYa8Nj8Gl3ikOBbMBG9zdA8xVUq\n" +
                "JzYkJFvn+lJSS9RHAa04L7nU08ivAgMBAAECgYBxAADJfWvhXY4z3iBUWZe3xDU6\n" +
                "/5AI9c/vvSd/BtQ1IhSj7XKrZlhF4EGqD3yJ88zc66PR4YeFTjJMObIcX+L/l0Ki\n" +
                "Yx+OyjthW7b9pVEjrUBc1Cbj26r8PzRCEM41zZy6jLB6M1Oi71hRYU6ZaetBnp2T\n" +
                "U2A5yUWoF2YT9VMSAQJBAOyEDRayskWsv9FIb9j0p42fLnhfMp4oUMvAQBQfRPEl\n" +
                "0wgbPHG2KHKeZCkpEkn1D6VFu8JAVkXNaz77Op9LiV0CQQDAY/5Gt+Wrjekz6myM\n" +
                "rDhRhoOmX6eSFCTzqOvZVLc/jy4aMLYC3VGmyiHOEXD5ONyE+LSKnbJxrK3vriKh\n" +
                "Cl17AkEA2KNXzcueaR2DkXHVKRdnhdwhV5ZzKdTptMeCqiu+HVg1BT7VTZ65S8tz\n" +
                "GRSKsP1r+El4YsRFgahXrJe3qYMp7QJBAL+TEoGC3y0sG3p5xWtyloX/xxolh+w7\n" +
                "KOyEWY3JAMxGm+ayeJtznLnT70OONIvGpje2m7in/SeahnzzTkJD2v8CQHWezzo3\n" +
                "m6NRXXDkbY7cLTnkk9zkQwkOjZO+/ChlQ6trrJ7UAKGL5Gj8x3gXbBrbU5mv7xL/\n" +
                "Py1ySQ4xSMT/gB0=\n" +
                "-----END PRIVATE KEY-----";

            certPem =
                "-----BEGIN CERTIFICATE-----\n" +
                "MIIC2zCCAcOgAwIBAgICBRcwDQYJKoZIhvcNAQELBQAwZDELMAkGA1UEBhMCVVMx\n" +
                "CzAJBgNVBAgMAkNBMRcwFQYDVQQHDA5QYWxvIEFsdG8gUm9vdDESMBAGA1UECgwJ\n" +
                "RmFrZSBSb290MRswGQYDVQQDDBJGYWtlIFJvb3QgUlNBIDIwNDgwHhcNMjEwNDE2\n" +
                "MTc0MjEwWhcNMzEwNDA0MTc0MjEwWjBpMQswCQYDVQQGEwJVUzELMAkGA1UECAwC\n" +
                "Q0ExEjAQBgNVBAcMCVBhbG8gQWx0bzEZMBcGA1UECgwQRmFrZSBBdHRlc3RhdGlv\n" +
                "bjEeMBwGA1UEAwwVRmFrZSBBdHRlc3RhdGlvbiAxMDI0MIGfMA0GCSqGSIb3DQEB\n" +
                "AQUAA4GNADCBiQKBgQCxv2vOcMt+rn+EZsKSP5UlIuG3LnhiWKHaVEvfQJwXiOhx\n" +
                "RCtowX9J4ij14ZB5zvwrmoxfMn2uu4ZB8QvuztdNiSKZKenqx5uqV4ki9gns1rem\n" +
                "cuwGo/C2GvDY/Bpd4pDgWzARvc3QPMVVKic2JCRb5/pSUkvURwGtOC+51NPIrwID\n" +
                "AQABoxYwFDASBgNVHRMBAf8ECDAGAQH/AgEBMA0GCSqGSIb3DQEBCwUAA4IBAQCr\n" +
                "L3VVxhYuGTyviLmcKxhIrQWRZgBfp+09bWtpoAjwYUGLjhaUpzh89N6ySUrNRPpT\n" +
                "vnN4aNwEPao6QafED1DLOreJSEbKeVAG0/NXT1LSioibmOBgqN9t9Kv4GqQjelgV\n" +
                "GNx1iZk2jLVqGPocpmoZbDXkIfaLB39Opm7yJyFKL5A6sTInTmysOagSO/zTI/3N\n" +
                "etoTT7KdwM99x6etAz+u8GAJqJ3Tdmp0RKWxM6V5FNXXRoDa1TxPLAzxOr1S5Bpb\n" +
                "Mdn3bhcKLsW0duJsKKVEFViQpqGJhvjEVZW0n2HXG+axvASArt6ADn5tZf/T4MQs\n" +
                "i0C0Pk5RDjcbuRYxqWJF\n" +
                "-----END CERTIFICATE-----";
        }

        // Get a pair that is valid, except the cert is version 1.
        public static void GetPair2048Version1(out string privateKeyPem, out string certPem)
        {
            privateKeyPem =
                "-----BEGIN PRIVATE KEY-----\n" +
                "MIIEwAIBADANBgkqhkiG9w0BAQEFAASCBKowggSmAgEAAoIBAQDbf2A4p7asu6sn\n" +
                "9Afk4B8mcZ1AkJqYp5VBf6A2xRXEsDxD/qBf8cTheaE6B6oL0zSz9Lfgub/X/A+e\n" +
                "8MscMbIDDm+9aaxFdo2AUQqPZ5F08rxRrLDTkyihwYcUciF4KO+24B5Ge5V3s6Tp\n" +
                "Klyu8IBeDH0NjZ7dsQ/krJVlGdwFSHWm3a1Ai4yJMzeh8Iq5gL6043vtD9A2bQzM\n" +
                "4kUWw9QhwJ/mMi9LR/YLxT3UACBvKUqrVFY5dxiccRsP97eR68P57ooseRrjprQ3\n" +
                "Gl/1ILw9W+VOqpXsuqA0lzFe0ag56IUufH168ua2eCzHyed0sugbuIx6yWD5duS4\n" +
                "TzP/rCSBAgMBAAECggEBAIRS/NoK2Yi2to8mgZ/MMVtGwQtMYbbHyKYs35RFKkNi\n" +
                "D2LaXSqaIRvg7H6EYlIwqKQYUsXqlSoLLguelIPRvcQz7s8cpptVxiZmNNyRDlKX\n" +
                "h0ohtpRGMgeaGOoNh/ndi+4OnJHXLRt1tGRQgoGAQZLxKm6CQxTZCoDMPmAtv8N6\n" +
                "dobrCgF4dmDZ2bPjGszn/c51tjdN2l8JtgOxp6LD+cK0sdkQCctn8W+xJLhXrBcX\n" +
                "JwJGqczX7cH9hvLRnCvTOJbRsb24b1YTPML4rQVkLzdtTWySCI5V6miWSl7q7HV/\n" +
                "kyloRv2Eqos0erat6Z2tyVCvuJRo1y5m0qnpd/+JXZUCgYEA75LITbF4XoC1MLpA\n" +
                "ZchbtUukHfjmiIUmiDX2QkopeyrCOEOwiDHMrq54/ZbIxwnePXHgyEhycDHyxki1\n" +
                "l7XlVgmqg8gX0lHFc0P6bNKSEQMIp+aKF2ZFFqNKV3I4gZnLL4+5+52ddWPsk1Qd\n" +
                "dVRdvbdtuzR6k0v2tGvd3jkRCvcCgYEA6ow0Nbrs5lJ2EQcbzB7Tpi/jJwAuKu/f\n" +
                "rKS2Z1HtqhqH9I9O7o2QbCoCMZKiJgpYRpdpy7h3cGAAUvLp7nniLiQJDrwtNQLk\n" +
                "arItdaoxLZyQhI8yEOBJ0lRbCq5ugbFN+NilJwQuSVM5uyNPcxgqdYGHFqF44LGQ\n" +
                "OZ/XCC7uNkcCgYEA7RKunpuRRstNAgQ9d7tWbUiGBpbo4o4IvF/R6nVjKRv+CBmL\n" +
                "1qqZJv9GgYO1+ajdQKaxTuDKRhZXbTpEYPXCFWsJTtEyKZF7t/28EfYqTyVWangr\n" +
                "jM5KbgV2qqRAIJflRpKO89xcFe+lC4IAiLvM69FZiBh9d8eDQbVAYAjOwa8CgYEA\n" +
                "xWCtQxYF7CEyyEuSIelDNRQRdS2arHlmYpPOCA6TEVX4WV8MDoZFJjEH3Y3HNHn6\n" +
                "JZWf61dV89RmEWfoYs5g/3FFygejh3vimsNMrDtH3Vlm6JbUjA0jMoPYhZma1ztN\n" +
                "IX+3I6lKBlyqNYiWgIWynWYeN3Y1Eel7NHMFcxaDUlsCgYEAvZ4e7yo9iz9gmtri\n" +
                "hlD0QnuM/5MLdczSwtDeZuGnalOAWWPGWMPp+SJ7t2sU+Xe/ytVGDQWmc/tzKoGG\n" +
                "rG7dSk9mkT4rod2gfKZdHdAsTQjvbx8lrezSgRHGYQF991Z4GTSTO2pe3r/LwhjP\n" +
                "iZZjh4mwb45JukjeGimRP9H3aio=\n" +
                "-----END PRIVATE KEY-----";

            certPem =
                "-----BEGIN CERTIFICATE-----\n" +
                "MIIDQjCCAioCAgUYMA0GCSqGSIb3DQEBCwUAMGQxCzAJBgNVBAYTAlVTMQswCQYD\n" +
                "VQQIDAJDQTEXMBUGA1UEBwwOUGFsbyBBbHRvIFJvb3QxEjAQBgNVBAoMCUZha2Ug\n" +
                "Um9vdDEbMBkGA1UEAwwSRmFrZSBSb290IFJTQSAyMDQ4MB4XDTIxMDQxNjE3NDIx\n" +
                "NVoXDTMxMDQwNDE3NDIxNVowaTELMAkGA1UEBhMCVVMxCzAJBgNVBAgMAkNBMRIw\n" +
                "EAYDVQQHDAlQYWxvIEFsdG8xGTAXBgNVBAoMEEZha2UgQXR0ZXN0YXRpb24xHjAc\n" +
                "BgNVBAMMFUZha2UgQXR0ZXN0YXRpb24gMjA0ODCCASIwDQYJKoZIhvcNAQEBBQAD\n" +
                "ggEPADCCAQoCggEBANt/YDintqy7qyf0B+TgHyZxnUCQmpinlUF/oDbFFcSwPEP+\n" +
                "oF/xxOF5oToHqgvTNLP0t+C5v9f8D57wyxwxsgMOb71prEV2jYBRCo9nkXTyvFGs\n" +
                "sNOTKKHBhxRyIXgo77bgHkZ7lXezpOkqXK7wgF4MfQ2Nnt2xD+SslWUZ3AVIdabd\n" +
                "rUCLjIkzN6HwirmAvrTje+0P0DZtDMziRRbD1CHAn+YyL0tH9gvFPdQAIG8pSqtU\n" +
                "Vjl3GJxxGw/3t5Hrw/nuiix5GuOmtDcaX/UgvD1b5U6qley6oDSXMV7RqDnohS58\n" +
                "fXry5rZ4LMfJ53Sy6Bu4jHrJYPl25LhPM/+sJIECAwEAATANBgkqhkiG9w0BAQsF\n" +
                "AAOCAQEAZZwTv0VapJd4Wbcdr3fI/sarBVw6NTrqcK85LdkGF8Nyh+RwNjwYoCjM\n" +
                "0x7PQ4w6CDzHsoy5LgHVh2i6EDrwKWTuJEQjxzKmzbGy5CVC6WenlQRs54GSkAiK\n" +
                "t6S38Z93mlotFD3bQ6aRC70yRZhm392dD3TJoJ04I6ut9h1C3/AVPEAD+S7DBa+j\n" +
                "HOLCfWP3fsFcf0vtKLbGnwz/OJdVVk8qW2SRbue7fYEqnC/10oYin2pAi7QMunFJ\n" +
                "l8YdWAD7z59wslyIzXi5WivlB592+P78xFNl/QS00nKzO9eXv3/HM006Vb9BLmK0\n" +
                "WvIT54DcVb+MwcRQvCyzgcWxekMtPg==\n" +
                "-----END CERTIFICATE-----";
        }

        // Get a pair that is valid, except the cert is version 1.
        public static void GetPair256Version1(out string privateKeyPem, out string certPem)
        {
            privateKeyPem =
                "-----BEGIN PRIVATE KEY-----\n" +
                "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgIEdISnMgVtpVb28B\n" +
                "8mxlrQ7eHtw8WbtvuV6BaK6jm/yhRANCAASATc5kxXt5D5v6oVIN1DlMJD2mCDb1\n" +
                "3GZQZfzujCFsEeBT5GuU13leL+p9yOXSoPXIm4zS+Dmbg2OpPfPHqrT5\n" +
                "-----END PRIVATE KEY-----";

            certPem =
                "-----BEGIN CERTIFICATE-----\n" +
                "MIICdjCCAV4CAgUSMA0GCSqGSIb3DQEBCwUAMGQxCzAJBgNVBAYTAlVTMQswCQYD\n" +
                "VQQIDAJDQTEXMBUGA1UEBwwOUGFsbyBBbHRvIFJvb3QxEjAQBgNVBAoMCUZha2Ug\n" +
                "Um9vdDEbMBkGA1UEAwwSRmFrZSBSb290IFJTQSAyMDQ4MB4XDTIxMDQxNjE3NDE0\n" +
                "NFoXDTMxMDQwNDE3NDE0NFowaDELMAkGA1UEBhMCVVMxCzAJBgNVBAgMAkNBMRIw\n" +
                "EAYDVQQHDAlQYWxvIEFsdG8xGTAXBgNVBAoMEEZha2UgQXR0ZXN0YXRpb24xHTAb\n" +
                "BgNVBAMMFEZha2UgQXR0ZXN0YXRpb24gMjU2MFkwEwYHKoZIzj0CAQYIKoZIzj0D\n" +
                "AQcDQgAEgE3OZMV7eQ+b+qFSDdQ5TCQ9pgg29dxmUGX87owhbBHgU+RrlNd5Xi/q\n" +
                "fcjl0qD1yJuM0vg5m4NjqT3zx6q0+TANBgkqhkiG9w0BAQsFAAOCAQEAlzLV+ZSB\n" +
                "ORU3s/qKA/uzC414Ilpe66BXAvc+trgJZk2FmUrhnvzr/JTCcgL5knjBlRsM2/CW\n" +
                "oNAVqNvyQTzv5nHEVrNhWzTZNzvO54NWP7EMJ7d0tZVn0brEycnPu8MLGshS7Hgz\n" +
                "gfWtLptAjJx6d7aJlDp5EupNZre51fViRuVKwB9f2dgvm9q2jtoMZ9+YdnCwzB/5\n" +
                "NXjE/O7CTw53kelxKEY4AbWdMOhMG+WQtbYbe7Pk1KM9EWdiHNg7dX1jViD8ysVP\n" +
                "9kblGWuh+OCNzurVvHtZQswIseNareOwVk13Mqk7Pq9zVrMi9Qn+rpjlSiTfop/W\n" +
                "+eK+8+M1p6TEZA==\n" +
                "-----END CERTIFICATE-----";
        }

        // Get a pair that is valid, except the cert is version 1.
        public static void GetPair384Version1(out string privateKeyPem, out string certPem)
        {
            privateKeyPem =
                "-----BEGIN PRIVATE KEY-----\n" +
                "MIG2AgEAMBAGByqGSM49AgEGBSuBBAAiBIGeMIGbAgEBBDAKroxlCShQ6sz89gMc\n" +
                "K09DIyI8kxaRZt9GlCMmbbmVpGhqhQLvwVdowQkA0xQ9A3+hZANiAAQMJtrJS7oU\n" +
                "Vxb9ofXTeGWHRzDyz+DEzktNNP32w1lk4W1xJYR7R0UjuhDiRkc7wC4e3UWN+wHU\n" +
                "GLtodeuMLnnxvp40psR3k/SVUbCn6UP0QFF/JOTv9fGtqfccBGVNHt8=\n" +
                "-----END PRIVATE KEY-----";

            certPem =
                "-----BEGIN CERTIFICATE-----\n" +
                "MIICkzCCAXsCAgUUMA0GCSqGSIb3DQEBCwUAMGQxCzAJBgNVBAYTAlVTMQswCQYD\n" +
                "VQQIDAJDQTEXMBUGA1UEBwwOUGFsbyBBbHRvIFJvb3QxEjAQBgNVBAoMCUZha2Ug\n" +
                "Um9vdDEbMBkGA1UEAwwSRmFrZSBSb290IFJTQSAyMDQ4MB4XDTIxMDQxNjE3NDIw\n" +
                "NVoXDTMxMDQwNDE3NDIwNVowaDELMAkGA1UEBhMCVVMxCzAJBgNVBAgMAkNBMRIw\n" +
                "EAYDVQQHDAlQYWxvIEFsdG8xGTAXBgNVBAoMEEZha2UgQXR0ZXN0YXRpb24xHTAb\n" +
                "BgNVBAMMFEZha2UgQXR0ZXN0YXRpb24gMzg0MHYwEAYHKoZIzj0CAQYFK4EEACID\n" +
                "YgAEDCbayUu6FFcW/aH103hlh0cw8s/gxM5LTTT99sNZZOFtcSWEe0dFI7oQ4kZH\n" +
                "O8AuHt1FjfsB1Bi7aHXrjC558b6eNKbEd5P0lVGwp+lD9EBRfyTk7/Xxran3HARl\n" +
                "TR7fMA0GCSqGSIb3DQEBCwUAA4IBAQAvioQty65EJejEJjxY4u4poMsEKC++HTzF\n" +
                "RcLB0zkWxcO4oxzDW11gogjAslA4QSfop79P33ln4uZ3aDHczEhguFcnJQ9Takwn\n" +
                "FQsXHOHCL2HupDyaQMznjPZrJYcv9jTUtSJ7IVQP8xYnN2eKi9vB5FeKL1UphM/B\n" +
                "FMUqrIsZIcL+sCi0Be1skAj82/+C+ny9GOEriMRkMN/WoAscuNIIP/E2JX1kCJbw\n" +
                "uJMBWPe8kGuzUsJ+iblLvTOd2dwDu5EtTJcESW+2zzwwSW1O41aS36ARrct/A3rQ\n" +
                "e510vuxfCvR7kt74bSuKi3wxsCTLtMEfIh51k3xZsa4FoLO8mm4v\n" +
                "-----END CERTIFICATE-----";
        }

        // Get a valid P256 key and a valid P384 cert.
        public static void GetMix256And384(out string privateKeyPem, out string certPem)
        {
            privateKeyPem =
                "-----BEGIN PRIVATE KEY-----\n" +
                "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgIEdISnMgVtpVb28B\n" +
                "8mxlrQ7eHtw8WbtvuV6BaK6jm/yhRANCAASATc5kxXt5D5v6oVIN1DlMJD2mCDb1\n" +
                "3GZQZfzujCFsEeBT5GuU13leL+p9yOXSoPXIm4zS+Dmbg2OpPfPHqrT5\n" +
                "-----END PRIVATE KEY-----";

            certPem =
                "-----BEGIN CERTIFICATE-----\n" +
                "MIICsDCCAZigAwIBAgICBRUwDQYJKoZIhvcNAQELBQAwZDELMAkGA1UEBhMCVVMx\n" +
                "CzAJBgNVBAgMAkNBMRcwFQYDVQQHDA5QYWxvIEFsdG8gUm9vdDESMBAGA1UECgwJ\n" +
                "RmFrZSBSb290MRswGQYDVQQDDBJGYWtlIFJvb3QgUlNBIDIwNDgwHhcNMjEwNDE2\n" +
                "MTc0MjA1WhcNMzEwNDA0MTc0MjA1WjBoMQswCQYDVQQGEwJVUzELMAkGA1UECAwC\n" +
                "Q0ExEjAQBgNVBAcMCVBhbG8gQWx0bzEZMBcGA1UECgwQRmFrZSBBdHRlc3RhdGlv\n" +
                "bjEdMBsGA1UEAwwURmFrZSBBdHRlc3RhdGlvbiAzODQwdjAQBgcqhkjOPQIBBgUr\n" +
                "gQQAIgNiAAQMJtrJS7oUVxb9ofXTeGWHRzDyz+DEzktNNP32w1lk4W1xJYR7R0Uj\n" +
                "uhDiRkc7wC4e3UWN+wHUGLtodeuMLnnxvp40psR3k/SVUbCn6UP0QFF/JOTv9fGt\n" +
                "qfccBGVNHt+jFjAUMBIGA1UdEwEB/wQIMAYBAf8CAQEwDQYJKoZIhvcNAQELBQAD\n" +
                "ggEBAKoM0ZlWkh11NtpzL46F/JOYzBbptS+CJiEC4SAZwYDEZrW7zkGko8rBVO8q\n" +
                "HpzRcNP88hW7YKHsrmTX3U3zJJZ96VxHT0R6zXMsZeOmkGT4tvjarGU2KJKKmN0Q\n" +
                "aRdIqiUApTcvBVICXJPJeAmIClQZ1AdMWf0sijikh5eiq44PkuJNj6gCu0UzZguB\n" +
                "Tio6GosI4lH58YviZi0WfyM19MS9MWLg3SGJniUwwI57+15Z5979IcUlC37UXLCY\n" +
                "oBn8zsluxvYqdKlFbUhy1x6C2UT2YWOzkqpBHtcC1uNG/AnnnL695WASdIw+qmd4\n" +
                "I5e05u1HmEVWQGbtX+DXtkrEGgw=\n" +
                "-----END CERTIFICATE-----";
        }

        // Get a valid RSA 2048 key and a valid RSA 2048 cert, but for a
        // different key.
        public static void GetMix2048And2048(out string privateKeyPem, out string certPem)
        {
            privateKeyPem =
                "-----BEGIN PRIVATE KEY-----\n" +
                "MIIEvwIBADANBgkqhkiG9w0BAQEFAASCBKkwggSlAgEAAoIBAQDRv3Wx6LMMfqZ3\n" +
                "1o0izERMdMNF9bMNv1CSwllvQmF3scuGpeDmLQraxBk4nkxQQX/LS/VzHVvBhiYo\n" +
                "QqbtJ+XxiuAGL46mXKYl3x4MJTfEeD47M6JdOm4aWZMFK/xeDpySYZs2qncR5ZfQ\n" +
                "iBd/OAmbBjH5N28DedrUJY7w3SBI6iB12u+RLouF83MkNMvhgznB0NoMGchqtBcM\n" +
                "v/0YDCOKDd2olq70cp0Ei49zF8G5pUm1TY18cxcEQrRzeEbWFCP5ez0bg+ELRrCk\n" +
                "1fkGLccQ+f9kASVY/CeTgvMw+y7fRIV9R4NocG4PCgqf7IB99KHvhaFF2QncAr7p\n" +
                "3P3H5kmpAgMBAAECggEBAIATWvjB0OMmStwORKwk8ueEvOBxQV55neefiSDo9b8y\n" +
                "78ZOb9/dTS18ZLIv5wVymWg3/67FFIw9L/uRh4B0xnIRjO36CC8Jj+K8NQrRhxYP\n" +
                "HmDkDJbE4Qpx+9ZCn52Hao/vzek5ee+RtHv/PenO+/6Pb+BvuvfyZm74aConvFkI\n" +
                "kn/aZz7MIw/0OV8TFru8ekKM5PW1fmzgoU9GZmSdwq5JGkB21qejtOnTH0YzD/FV\n" +
                "w4QBse0LXjGrp1qJ+tEGXjp5xiiIrOTcyQw9D56bZWKPGLMMp6KyvWay/x9x/ToT\n" +
                "D84WL/Lk3DgXBAFQzTpfVASVj+UDRcwUeALVu8ALnAECgYEA8af0MrKEt+CryDOw\n" +
                "TbFq9RTqkXW+wtQHy8x7M+9Oxj4RaX2/evYWneX0KUc4o8WveEfv1mIueGFb/3WM\n" +
                "TEqCti3/AFWI6NdU0EbxNbzteYaMAIW8fovE3H5pyErQvwX6Ees0OE2BeIfR9M1w\n" +
                "MTTOavto9Xg8bf+dur+Bgyal1sECgYEA3jKmc9UiuG6DKk+JlhUNhWhdvxv9D1+w\n" +
                "fUBmA7mEtAdYrxz6PUd2nirwB9S6UeHbtl5tbVBAYHmqlI6njygcYQ8nbt+nLwd5\n" +
                "XjeZiJnfvqrvz4qZE46MIZto479ajT/i0aBkZVbbQvp8wFd9R1H5R15/sgsHibXE\n" +
                "XwWWnDqe1OkCgYEA47T5PBRPTtzbwYhDJtJ5EHsnFO24VOlqdzU1Gpjyx4aQ7bBa\n" +
                "D8l3Qk3+pi7ARkHuuA5BBuf5FeHXyH3BN9o2FOh+kpgGrDDLcH6Ip7RgqNSJc6yR\n" +
                "E0UsuQA9OUiWLom5O80/pZYS27pPsrcqcpNpthE0s6kaeCQXQnNV3Hk8Z4ECgYBx\n" +
                "v6AywDOsEvcW2+zlZhWr7AfB5AQisKvbEvKmiXyD5RbjXoREhqcUxYpnl+FiNauS\n" +
                "qrh+M40hVmea8YSZ5sDQdz+KpPgjPUJGl1QD+DHwm/V0W9GNj3XxZmvF25nxoXju\n" +
                "M5vxvQs2OKFQnflGX5KrlJbugHL1bpX+xw+ZHvFcsQKBgQCkGZvXWkpjOMHrurCO\n" +
                "zrZ4WbHHOQd2/u0sLwbLizvBNmGFsVbN5EODEM7PmoWKSV9t0LA2BI01ZD995H4E\n" +
                "yJUvUabI7atWcXS0hYgQtvXASH3uiqWITlo6/eWx9tgO9WaHNrMBFKzlnE67PbDE\n" +
                "rqPbTi8glealTGT31SC8G2YuBw==\n" +
                "-----END PRIVATE KEY-----";

            certPem =
                "-----BEGIN CERTIFICATE-----\n" +
                "MIIDXzCCAkegAwIBAgICBRkwDQYJKoZIhvcNAQELBQAwZDELMAkGA1UEBhMCVVMx\n" +
                "CzAJBgNVBAgMAkNBMRcwFQYDVQQHDA5QYWxvIEFsdG8gUm9vdDESMBAGA1UECgwJ\n" +
                "RmFrZSBSb290MRswGQYDVQQDDBJGYWtlIFJvb3QgUlNBIDIwNDgwHhcNMjEwNDE2\n" +
                "MTc0MjE1WhcNMzEwNDA0MTc0MjE1WjBpMQswCQYDVQQGEwJVUzELMAkGA1UECAwC\n" +
                "Q0ExEjAQBgNVBAcMCVBhbG8gQWx0bzEZMBcGA1UECgwQRmFrZSBBdHRlc3RhdGlv\n" +
                "bjEeMBwGA1UEAwwVRmFrZSBBdHRlc3RhdGlvbiAyMDQ4MIIBIjANBgkqhkiG9w0B\n" +
                "AQEFAAOCAQ8AMIIBCgKCAQEA239gOKe2rLurJ/QH5OAfJnGdQJCamKeVQX+gNsUV\n" +
                "xLA8Q/6gX/HE4XmhOgeqC9M0s/S34Lm/1/wPnvDLHDGyAw5vvWmsRXaNgFEKj2eR\n" +
                "dPK8Uayw05MoocGHFHIheCjvtuAeRnuVd7Ok6SpcrvCAXgx9DY2e3bEP5KyVZRnc\n" +
                "BUh1pt2tQIuMiTM3ofCKuYC+tON77Q/QNm0MzOJFFsPUIcCf5jIvS0f2C8U91AAg\n" +
                "bylKq1RWOXcYnHEbD/e3kevD+e6KLHka46a0Nxpf9SC8PVvlTqqV7LqgNJcxXtGo\n" +
                "OeiFLnx9evLmtngsx8nndLLoG7iMeslg+XbkuE8z/6wkgQIDAQABoxYwFDASBgNV\n" +
                "HRMBAf8ECDAGAQH/AgEBMA0GCSqGSIb3DQEBCwUAA4IBAQBkJB6CYu/+2NQbnQ69\n" +
                "B2XXaR6AXxyL8XVB/d91Ei4ZViloFUZY4jpJ7yAEN+U6R824V/WWDMGhoIgm5u1L\n" +
                "qTfi+Uqc6lTHNxWEP4B7nH1VOgh8ego9anenkTWtr6m+RTwJF3TpfIuZTJPkdU3Z\n" +
                "eFyv3OQ9TWLxwGQOqT1Mx8km/xM18PawCcrYXX3AHddYUBtdPEAVfDakWn6coL1+\n" +
                "oXKVHI79VrTGOKx1W0ZQefwlz7OaJn3JaBlJzUys/dXwpqnbRogiZoHLSK3uEMga\n" +
                "JkKe+c+ul+dWtd3ykTWaciknQILyFwNu+MbEYOATl4BGR+/gOrw5hmAsfdV3bde0\n" +
                "R9WM\n" +
                "-----END CERTIFICATE-----";
        }

        public static void Get2048BigIssuerName(out string privateKeyPem, out string certPem)
        {
            privateKeyPem =
                "-----BEGIN PRIVATE KEY-----\n" +
                "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCz/caibUmGZrk0\n" +
                "VhOWpQzV6O87oQh0EJCHTM4P9z6AJG+gqoL8V6aYcGnXiEg9LQu8JsP8679eje2v\n" +
                "2+ca/N51gVlOX2SGFQpK3RAzd7pkFZkn3RKyeSNAmUb+H25H2rNONNkNib5CYGFb\n" +
                "IICieCXgCnUgpYBPEPoqIcQNHmUP6M8Sz/BmcmiWdvHQmPLQ48jk0NcL7l+3/Nrq\n" +
                "hJ3pXd6Cr3GQz7EGTAMS1zlpVyZrYfhRvdE2hArBHr9tE0KAPMLnXkCBjUgyEJWV\n" +
                "uYRhAgt8aLqrgeYzru6ytPlBXlB6qmB8lIkIqKMayIlmQ4Lp+Zq0p5Ko7Mxpin1H\n" +
                "osG1rc7lAgMBAAECggEANNDwG7OUErNL/3aOsvLlzFNY+Bdt4pkFwB4ijX7QwUtv\n" +
                "0iaW3zNdOHgsJsnf4Mu6GNELS8ll03o0WBlgPIQdRz/Yk+3cEphT99ncqi2k7T+F\n" +
                "PLRbizGOzaLsuR9B/iXH3dgWJSnZQaMEjngAJyy4eIC5FAZcm1bxAbH81JipsTvy\n" +
                "hyi2ge7DZ2RLKQFsJFMu6ghvBHOjlbhVJTQJqh0TDrZ8wwLYoypPk9dmbKNM5LpL\n" +
                "yz1jVKgF3yT5dGSajkTh/Qr6X9UawTq8M8chuGG5zExtDWxy/sXEAqU68jxuqIZL\n" +
                "kb0bDFTOK/FNfm6kl+13eDGlenOoNy+ElWR/LABU1QKBgQDgGbLUUmE1tuu3KX4E\n" +
                "kH7Ixl0tstRKXblWwQ37FvDx6algbPfyA3E2wizP2Smgxslb2D/SjiH6m8aro9tN\n" +
                "o3LuhyM4FLsAYW8u4m3DuTMSg68a1ZvCXkO6Y1cm/UdhwRBIajFDLp6+Cw0URHzA\n" +
                "ZaTERzmrbW1ar4E6N/ONjQ9EpwKBgQDNnLnw33SG9kjV6wcbfRDJXU5BK85ntUqL\n" +
                "9KBUmX6FKuahoiaVYocOhrLgCb7GFc+RobEGVozrkA3Go5XRp4Ix9Ylhf0PnD2A/\n" +
                "TcZ+kZXIUQUBL+FeZx67D3CYswnqOebtwMb300Bmanxu+Nqe6djArjCbSXyc/gX2\n" +
                "tKuWtoDlkwKBgQDAQivi7h4J+Dm3tPgxMEolM9FC4HYyqr/UBuJYtTDXSjCO0k3R\n" +
                "qlRZtzK8ysFk9sZPbnIq0Nej6jsCjBwcOoriyrtTZK2eQPkjDw0+ake/rYvviK0N\n" +
                "jtOqN4nQoGC6I+k1Ry2mRnvX7SE2bx9b7Jfz8GswgDveHk3OxavEl+0uZwKBgDth\n" +
                "avXcovujPw/Aq7HNob512vbJXvfmjJv0zyT/m2F8LVU6zifQZ67TSe+YAOeWPvcR\n" +
                "Gl35OwOA++mFLux4kwo4ni9xILwnXaWKoavGAdrzQx2/pTetUlu1rs/6zP8/L6k0\n" +
                "RoImGXA6iqtF5WWFpZqn89O1Gm8AkdpY/UEuffVPAoGBAJ1AQN5tuFngzKNnsYSn\n" +
                "vukXj9u8OwtuKgOhSx1peXdwMevNhahg2CoMR3wrvC4km/75ptDklSlCc76jo9Cu\n" +
                "Ju9Zisiea7rTRuE7TMtAWZXMmK9GPjss0C64u8MCmFhcVfH4HTsJiqB73zr2nUgj\n" +
                "T4Abht+hbNuO77fC6K8qpuX6\n" +
                "-----END PRIVATE KEY-----";

            certPem =
                "-----BEGIN CERTIFICATE-----\n" +
                "MIIHPTCCBiWgAwIBAgIJAPV+r/S49uJVMA0GCSqGSIb3DQEBCwUAMGQxCzAJBgNV\n" +
                "BAYTAlVTMQswCQYDVQQIDAJDQTEXMBUGA1UEBwwOUGFsbyBBbHRvIFJvb3QxEjAQ\n" +
                "BgNVBAoMCUZha2UgUm9vdDEbMBkGA1UEAwwSRmFrZSBSb290IFJTQSAyMDQ4MB4X\n" +
                "DTIxMDIyNzAwMDUzMloXDTMxMDIyNTAwMDUzMlowggQBMQswCQYDVQQGEwJVUzEL\n" +
                "MAkGA1UECAwCQ0ExFzAVBgNVBAcMDlBhbG8gQWx0byBSb290MRIwEAYDVQQKDAlG\n" +
                "YWtlIFJvb3QxGzAZBgNVBAMMEkZha2UgUm9vdCBSU0EgMjA0ODGCA5kwggOVBgNV\n" +
                "BA8MggOMMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0RFRkdIMjIzNDU2NzhBQkNERUZHSDIyMzQ1Njc4QUJDREVGR0gyMjM0NTY3OEFC\n" +
                "Q0QwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC3Gv0eQ9wyK7lUPqun\n" +
                "9NiKu/nRd/FXVByk/CW9bC2WFU9MZU9zXbs9YIf3ErihDjEtDxMHPBiOQc3TSqa5\n" +
                "EByUhQC4FPyiK+KNuGYnO++9ItXD2Le0j9sh7g7wX2u6+fLuT4n5oYMczrDlZbEa\n" +
                "g4vxNo+rB+AyAp87fS13OVl5vLETESCjuEZ9kmKH2V6eQQxerYc8Bu3g5BfxOqkY\n" +
                "xLlRxwDJAxCCUMF1WV7ShpNQggtvXKTHIjcow93oJviKgj3ewZOUMPB/KKqm7y/N\n" +
                "w7EhZx+f+KJn866qtcUWrWG6zS/d/TsU/ghfWEOei1K3Zlue3FZ7zUt/941F/1t5\n" +
                "EzFdAgMBAAGjUzBRMB0GA1UdDgQWBBTRapRVM6TkH7GuWO8KtMr/PRWoEjAfBgNV\n" +
                "HSMEGDAWgBTRapRVM6TkH7GuWO8KtMr/PRWoEjAPBgNVHRMBAf8EBTADAQH/MA0G\n" +
                "CSqGSIb3DQEBCwUAA4IBAQCiViCkRg4/Sfvge6dnhpeNjUilFczPdij3FtlXBGqZ\n" +
                "jjPg1Xdw3Ncu+THGp9p2rtYU093wohl/S4BHoAvRO0GnwU4LdoGx3u00CyELLndv\n" +
                "RpNOh2goYUmawiyU5ix9hVSz5JyOYQs8XTe6F6GkOLsGMM6shZZLJVGP8l4iizd3\n" +
                "ZVaC5Z1L1b8S60ZiKuAj1j3NBu0x3ppihC63JMsapxR9MlCu/uFKa0WKDE7/f1YO\n" +
                "IwzC5jPttjaT4yxUSIKQLzmV815i7vu/s/dhq5F7B5m2F8um4PhcgyQF+gwx5KkQ\n" +
                "rE5JOT7BUzSRrwSKlaHBCycEi535byD4wdQeXPacnWnx\n" +
                "-----END CERTIFICATE-----";
        }
    }
}
