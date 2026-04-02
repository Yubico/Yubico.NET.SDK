// Copyright 2025 Yubico AB
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

using System.Text;

namespace Yubico.YubiKit.Core.UnitTests.SmartCard.Scp;

/// <summary>
///     Test data for SCP11 unit tests.
///     Contains example OCE (Off-Card Entity) certificates and private keys for testing SCP11a/b/c.
/// </summary>
internal static class Scp11TestData
{
    /// <summary>
    ///     Example OCE certificate chain in PEM format.
    ///     Contains intermediate and root CA certificates.
    /// </summary>
    public static readonly ReadOnlyMemory<byte> OceCerts = Encoding.UTF8.GetBytes("""
        -----BEGIN CERTIFICATE-----
        MIIB8DCCAZegAwIBAgIUf0lxsK1R+EydqZKLLV/vXhaykgowCgYIKoZIzj0EAwIw
        KjEoMCYGA1UEAwwfRXhhbXBsZSBPQ0UgUm9vdCBDQSBDZXJ0aWZpY2F0ZTAeFw0y
        NDA1MjgwOTIyMDlaFw0yNDA4MjYwOTIyMDlaMC8xLTArBgNVBAMMJEV4YW1wbGUg
        T0NFIEludGVybWVkaWF0ZSBDZXJ0aWZpY2F0ZTBZMBMGByqGSM49AgEGCCqGSM49
        AwEHA0IABMXbjb+Y33+GP8qUznrdZSJX9b2qC0VUS1WDhuTlQUfg/RBNFXb2/qWt
        h/a+Ag406fV7wZW2e4PPH+Le7EwS1nyjgZUwgZIwHQYDVR0OBBYEFJzdQCINVBES
        R4yZBN2l5CXyzlWsMB8GA1UdIwQYMBaAFDGqVWafYGfoHzPc/QT+3nPlcZ89MBIG
        A1UdEwEB/wQIMAYBAf8CAQAwDgYDVR0PAQH/BAQDAgIEMCwGA1UdIAEB/wQiMCAw
        DgYMKoZIhvxrZAAKAgEoMA4GDCqGSIb8a2QACgIBADAKBggqhkjOPQQDAgNHADBE
        AiBE5SpNEKDW3OehDhvTKT9g1cuuIyPdaXGLZ3iX0x0VcwIgdnIirhlKocOKGXf9
        ijkE8e+9dTazSPLf24lSIf0IGC8=
        -----END CERTIFICATE-----
        -----BEGIN CERTIFICATE-----
        MIIB2zCCAYGgAwIBAgIUSf59wIpCKOrNGNc5FMPTD9zDGVAwCgYIKoZIzj0EAwIw
        KjEoMCYGA1UEAwwfRXhhbXBsZSBPQ0UgUm9vdCBDQSBDZXJ0aWZpY2F0ZTAeFw0y
        NDA1MjgwOTIyMDlaFw0yNDA2MjcwOTIyMDlaMCoxKDAmBgNVBAMMH0V4YW1wbGUg
        T0NFIFJvb3QgQ0EgQ2VydGlmaWNhdGUwWTATBgcqhkjOPQIBBggqhkjOPQMBBwNC
        AASPrxfpSB/AvuvLKaCz1YTx68Xbtx8S9xAMfRGwzp5cXMdF8c7AWpUfeM3BQ26M
        h0WPvyBJKhCdeK8iVCaHyr5Jo4GEMIGBMB0GA1UdDgQWBBQxqlVmn2Bn6B8z3P0E
        /t5z5XGfPTASBgNVHRMBAf8ECDAGAQH/AgEBMA4GA1UdDwEB/wQEAwIBBjA8BgNV
        HSABAf8EMjAwMA4GDCqGSIb8a2QACgIBFDAOBgwqhkiG/GtkAAoCASgwDgYMKoZI
        hvxrZAAKAgEAMAoGCCqGSM49BAMCA0gAMEUCIHv8cgOzxq2n1uZktL9gCXSR85mk
        TieYeSoKZn6MM4rOAiEA1S/+7ez/gxDl01ztKeoHiUiW4FbEG4JUCzIITaGxVvM=
        -----END CERTIFICATE-----
        """);

    /// <summary>
    ///     PKCS12 container with OCE private key and full certificate chain.
    ///     Password: "password"
    /// </summary>
    public static readonly Memory<byte> Oce = Convert.FromBase64String("""
                                                                       MIIIfAIBAzCCCDIGCSqGSIb3DQEHAaCCCCMEgggfMIIIGzCCBtIGCSqGSIb3DQEHBqCCBsMwgga/
                                                                       AgEAMIIGuAYJKoZIhvcNAQcBMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAg8IcJO44iS
                                                                       gAICCAAwDAYIKoZIhvcNAgkFADAdBglghkgBZQMEASoEEAllIHdoQx/USA3jmRMeciiAggZQAHCP
                                                                       J5lzPV0Z5tnssXZZ1AWm8AcKEq28gWUTVqVxc+0EcbKQHig1Jx7rqC3q4G4sboIRw1vDH6q5O8eG
                                                                       sbkeNuYBim8fZ08JrsjeJABJoEiJrPqplMWA7H6a7athg3YSu1v4OR3UKN5Gyzn3s0Yx5yMm/xzw
                                                                       204TEK5/1LpK8AMcUliFSq7jw3Xl1RY0zjMSWyQjX0KmB9IdubqQCfhy8zkKluAQADtHsEYAn0F3
                                                                       LoMETQytyUSkIvGMZoFemkCWV7zZ5n5IPhXL7gvnTu0WS8UxEnz/+FYdF43cjmwGfSb3OpaxOND4
                                                                       PBCpwzbFfVCLa6mUBlwq1KQWRm1+PFm4LnL+3s2mxfjJAsVYP4U722/FHpW8rdTsyvdift9lsQja
                                                                       s2jIjCu8PFClFZJLQldu5FxOhKzx2gsjYS/aeTdefwjlRiGtEFSrE1snKBbnBeRYFocBjhTD/sy3
                                                                       Vj0i5sbWwTx7iq67joWydWAMp/lGSZ6akWRsyku/282jlwYsc3pR05qCHkbV0TzJcZofhXBwRgH5
                                                                       NKfulnJ1gH+i3e3RT3TauAKlqCeAfvDvA3+jxEDy/puPncod7WH0m9P4OmXjZ0s5EI4U+v6bKPgL
                                                                       7LlTCEI6yj15P7kxmruoxZlDAmhixVmlwJ8ZbVxD6Q+AOhXYPg+il3AYaRAS+VyJla0K+ac6hpYV
                                                                       AnbZCPzgHVkKC6iq4a/azf2b4uq9ks109jjnryAChdBsGdmStpZaPW4koMSAIJf12vGRp5jNjSax
                                                                       aIL5QxTn0WCO8FHi1oqTmlTSWvR8wwZLiBmqQtnNTpewiLL7C22lerUT7pYvKLCq/nnPYtb5UrST
                                                                       HrmTNOUzEGVOSAGUWV293S4yiPGIwxT3dPE5/UaU/yKq1RonMRaPhOZEESZEwLKVCqyDVEbAt7Hd
                                                                       ahp+Ex0FVrC5JQhpVQ0Wn6uCptF2Jup70u+P2kVWjxrGBuRrlgEkKuHcohWoO9EMX/bLK9KcY4s1
                                                                       ofnfgSNagsAyX7N51Bmahgz1MCFOEcuFa375QYQhqkyLO2ZkNTpFQtjHjX0izZWO55LN3rNpcD9+
                                                                       fZt6ldoZCpg+t6y5xqHy+7soH0BpxF1oGIHAUkYSuXpLY0M7Pt3qqvsJ4/ycmFUEyoGv8Ib/ieUB
                                                                       bebPz0Uhn+jaTpjgtKCyym7nBxVCuUv39vZ31nhNr4WaFsjdB/FOJh1s4KI6kQgzCSObrIVXBcLC
                                                                       TXPfZ3jWxspKIREHn+zNuW7jIkbugSRiNFfVArcc7cmU4av9JPSmFiZzeyA0gkrkESTg8DVPT16u
                                                                       7W5HREX4CwmKu+12R6iYQ/po9Hcy6NJ8ShLdAzU0+q/BzgH7Cb8qimjgfGBA3Mesc+P98FlCzAjB
                                                                       2EgucRuXuehM/FemmZyNl0qI1Mj9qOgx/HeYaJaYD+yXwojApmetFGtDtMJsDxwL0zK7eGXeHHa7
                                                                       pd7OybKdSjDq25CCTOZvfR0DD55FDIGCy0FsJTcferzPFlkz/Q45vEwuGfEBnXXS9IhH4ySvJmDm
                                                                       yfLMGiHW6t+9gjyEEg+dwSOq9yXYScfCsefRl7+o/9nDoNQ8s/XS7LKlJ72ZEBaKeAxcm6q4wVwU
                                                                       WITNNl1R3EYAsFBWzYt4Ka9Ob3igVaNfeG9K4pfQqMWcPpqVp4FuIsEpDWZYuv71s+WMYCs1JMfH
                                                                       bHDUczdRet1Ir2vLDGeWwvci70AzeKvvQ9OwBVESRec6cVrgt3EJWLey5sXY01WpMm526fwtLolS
                                                                       MpCf+dNePT97nXemQCcr3QXimagHTSGPngG3577FPrSQJl+lCJDYxBFFtnd6hq4OcVr5HiNAbLnS
                                                                       jBWbzqxhHMmgoojy4rwtHmrfyVYKXyl+98r+Lobitv2tpnBqmjL6dMPRBOJvQl8+Wp4MGBsi1gvT
                                                                       gW/+pLlMXT++1iYyxBeK9/AN5hfjtrivewE3JY531jwkrl3rUl50MKwBJMMAtQQIYrDg7DAg/+Qc
                                                                       Oi+2mgo9zJPzR2jIXF0wP+9FA4+MITa2v78QVXcesh63agcFJCayGAL1StnbSBvvDqK5vEei3uGZ
                                                                       beJEpU1hikQx57w3UzS9O7OSQMFvRBOrFBQsYC4JzfF0soIweGNpJxpm+UNYz+hB9vCb8+3OHA06
                                                                       9M0CAlJVOTF9uEpLVRzK+1kwggFBBgkqhkiG9w0BBwGgggEyBIIBLjCCASowggEmBgsqhkiG9w0B
                                                                       DAoBAqCB7zCB7DBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQIexxrwNlHM34CAggAMAwG
                                                                       CCqGSIb3DQIJBQAwHQYJYIZIAWUDBAEqBBAkK96h6gHJglyJl1/yEylvBIGQh62z7u5RoQ9y5wIX
                                                                       bE3/oMQTKVfCSrtqGUmj38sxDY7yIoTVQq7sw0MPNeYHROgGUAzawU0DlXMGuOWrbgzYeURZs0/H
                                                                       Z2Cqk8qhVnD8TgpB2n0U0NB7aJRHlkzTl5MLFAwn3NE49CSzb891lGwfLYXYCfNfqltD7xZ7uvz6
                                                                       JAo/y6UtY8892wrRv4UdejyfMSUwIwYJKoZIhvcNAQkVMRYEFJBU0s1/6SLbIRbyeq65gLWqClWN
                                                                       MEEwMTANBglghkgBZQMEAgEFAAQgqkOJRTcBlnx5yn57k23PH+qUXUGPEuYkrGy+DzEQiikECB0B
                                                                       XjHOZZhuAgIIAA==
                                                                       """);

    /// <summary>
    ///     Password for the OCE PKCS12 container.
    /// </summary>
    public static readonly ReadOnlyMemory<char> OcePassword = "password".AsMemory();

    /// <summary>
    ///     Serial numbers from the OCE certificate chain.
    ///     Used for allowlist testing in SCP11a/c.
    /// </summary>
    public static readonly string[] OceCertSerialNumbers =
    [
        "7F4971B0AD51F84C9DA9928B2D5FEF5E16B2920A", // Intermediate cert
        "6B90028800909F9FFCD641346933242748FBE9AD" // Root CA cert
    ];
}