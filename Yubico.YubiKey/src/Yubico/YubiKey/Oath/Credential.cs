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
using System.Collections.Specialized;
using System.Web;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Convert;

namespace Yubico.YubiKey.Oath
{
    /// <summary>
    /// Represents a single OATH credential.
    /// </summary>
    /// <remarks>
    /// The credential can be a TOTP (Time-based One-time Password) or a HOTP (HMAC-based One-time Password).
    /// </remarks>
    public class Credential
    {
        private const int DefaultDigits = 6;
        private const int MaximumNameLength = 64;
        private const int MaximumUrlLength = 64;
        private const string uriScheme = "otpauth";

        private string? _issuer;
        private string? _accountName;
        private string? _secret;
        private int? _digits;
        private int? _counter;
        private CredentialType? _type;
        private CredentialPeriod? _period;
        private HashAlgorithm? _algorithm;

        /// <summary>
        /// The type of the credential. 
        /// Indicates the type of the credential as either HOTP or TOTP.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The value is invalid.
        /// </exception>
        public CredentialType? Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    if (!Enum.IsDefined(typeof(CredentialType), value))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.ValueIsNotEnum,
                                value,
                                nameof(CredentialType)));
                    }
                }
                _type = value;
            }
        }

        /// <summary>
        /// The hash algorithm used by the credential.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The value is invalid.
        /// </exception>
        public HashAlgorithm? Algorithm
        {
            get => _algorithm;
            set
            {
                if (_algorithm != value) 
                {
                    if (!Enum.IsDefined(typeof(HashAlgorithm), value))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                CultureInfo.CurrentCulture,
                                ExceptionMessages.ValueIsNotEnum,
                                value,
                                nameof(Algorithm)));
                    }
                }
                _algorithm = value;
            } 
        }

        /// <summary>
        /// String value indicating the provider or the service the account is associated with.
        /// </summary>
        /// <remarks>
        /// The issuer parameter is recommended, but it can be absent.
        /// </remarks>
        public string? Issuer
        {
            get => _issuer;
            set
            {
                string? issuer = value ?? string.Empty;
                CheckNameLength();
                _issuer = issuer;
            }
        }

        /// <summary>
        /// The account name is a string that usually is the user's email address.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The value is null, empty, or consists only of white-space characters.
        /// </exception>
        public string? AccountName
        {
            get => _accountName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException(ExceptionMessages.InvalidCredentialAccount);
                }

                CheckNameLength();
                _accountName = value;
            }
        }

        /// <summary>
        /// The secret is an arbitrary value encoded in Base32 according to RFC 3548.
        /// </summary>
        /// <remarks>
        /// Usually, the shared secret is provided by the provider or service website to the user
        /// by means of a QR code. Both sides need to retain this secret key for one-time password generation. The YubiKey takes care of securely storing this secret on behalf of the user when the credential is added. An authenticator app does not need to store this secret anywhere else.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The value is invalid.
        /// </exception>
        public string? Secret
        {
            get => _secret;
            set
            {
                if (value != null) {

                    var regexSecret = new Regex(@"[A-Za-z2-7=]*");

                    if (regexSecret.Match(value).Value != value)
                    {
                        throw new InvalidOperationException(ExceptionMessages.InvalidCredentialSecret);
                    }

                }
                
                _secret = value;
            }
        }

        /// <summary>
        /// The number of digits in a one-time password.
        /// The value for this property can only be 6, 7 or 8.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The value is invalid.
        /// </exception>
        public int? Digits
        {
            get => _digits;
            set
            {
                if (value < 6 || (value > 8))
                {
                    throw new InvalidOperationException(ExceptionMessages.InvalidCredentialDigits);
                }

                _digits = value;
            }
        }

        /// <summary>
        /// The validity period in seconds for TOTP code.
        /// It can only be 15, 30 or 60 seconds. For HOTP should be set to zero.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The value is invalid.
        /// </exception>
        public CredentialPeriod? Period
        {
            get => _period;

            set
            {
                if (Type == CredentialType.Totp) 
                {

                    if (!new CredentialPeriod?[] { CredentialPeriod.Period15, CredentialPeriod.Period30, CredentialPeriod.Period60 }.Contains(value))
                    {
                        throw new InvalidOperationException(ExceptionMessages.InvalidCredentialPeriod);
                    }
                }
                else
                {
                    if (value != CredentialPeriod.Undefined)
                    {
                        throw new InvalidOperationException(ExceptionMessages.InvalidCredentialPeriod);
                    }
                }

                CheckNameLength();
                _period = value;
            }
        }

        /// <summary>
        /// Counter value for HOTP.
        /// </summary>
        /// <remarks>
        /// The counter parameter is required when the type is HOTP. It will set the initial counter value.
        /// This property returns null if the credential type is TOTP.
        /// The server and user calculate the OTP by applying a hashing and truncating operation to Secret and Counter.
        /// The server compares the OTP it calculated against the one provided by the user. Both sides then increment the counters.
        /// The counters have to be kept in sync between the server and the user. If a user ends up not using calculated OTP,
        /// the counter on the user side will become out of sync with the server. 
        /// </remarks>
        public int? Counter
        {
            get => Type == CredentialType.Hotp ? _counter : null;
            set => _counter = value;
        }

        /// <summary>
        /// The credential requires the user to touch the key to generate a one-time password.
        /// </summary>
        public bool? RequiresTouch { get; set; }

        /// <summary>
        /// Get-property witch serves as the unique identifier for the credential.
        /// </summary>
        /// <remarks>
        /// The Name prevents collisions between different accounts with different providers that might be identified using
        /// the same account name, e.g. the user's email address. The Name is created from Period, Issue and Account Name with
        /// the following format: "period/issuer:account". If Period is a default value (30seconds), or the credential's type is HOTP,
        /// it'll be: "issuer:account". Also, if Issuer is not specified, the format will be: "period/account" or just "account" for TOTP
        /// with default period or HOTP credentials.
        /// </remarks>
        public string Name
        {
            get
            {
                var nameString = new StringBuilder();

                if (Type == CredentialType.Totp && Period != null && Period != CredentialPeriod.Period30)
                {
                    _ = nameString.Append($"{(int)Period}/");
                }

                if (Issuer != null)
                {
                    _ = nameString.Append($"{Issuer}:");
                }

                return nameString.Append(AccountName).ToString();
            }
        }

        /// <summary>
        /// Constructs an instance of the <see cref="Credential" /> class.
        /// </summary>
        public Credential()
        {
        }

        /// <summary>
        /// Constructs an instance of the <see cref="Credential" /> class for CalculateAllCommand.
        /// </summary>
        /// <param name="issuer">
        /// A string indicating the provider or service.
        /// </param>
        /// <param name="account">
        /// The account name that usually is the user's email address.
        /// </param>
        /// <param name="type">
        /// The credential type, TOTP or HOTP.
        /// </param>  
        /// <param name="period">
        /// The credential period.
        /// </param>
        public Credential(string issuer, string account, CredentialType type, CredentialPeriod period)
        {
            Issuer = issuer;
            AccountName = account;
            Type = type;
            Period = period;
        }

        /// <summary>
        /// Constructs an instance of the <see cref="Credential" /> class for List Command.
        /// </summary>
        /// <param name="issuer">
        /// The issuer is a string indicating the provider or service.
        /// </param>
        /// <param name="account">
        /// The account name that usually is the user's email address.
        /// </param> 
        /// <param name="period">
        /// The credential period.
        /// </param>
        /// <param name="type">
        /// The credential type, TOTP or HOTP.
        /// </param> 
        /// <param name="algorithm">
        /// The types of hash algorithm.
        /// </param> 
        public Credential(string issuer, string account, CredentialPeriod period, CredentialType type, HashAlgorithm algorithm)
        {
            Issuer = issuer;
            AccountName = account;
            Type = type;
            Period = period;
            Algorithm = algorithm;
        }

        /// <summary>
        /// Constructs an instance of the <see cref="Credential" /> class for PutCommand.
        /// </summary>
        /// <param name="issuer">
        /// The issuer is a string indicating the provider or service.
        /// </param>
        /// <param name="account">
        /// The account name that usually is the user's email address.
        /// </param> 
        /// <param name="type">
        /// The credential type, TOTP or HOTP.
        /// </param>
        /// <param name="algorithm">
        /// The type of hash algorithm.
        /// </param>
        /// <param name="secret">
        /// An arbitrary value.
        /// </param> 
        /// <param name="period">
        /// The credential period.
        /// </param>
        /// <param name="digits">
        /// The number of digits in a one-time password.
        /// </param>
        /// <param name="counter">
        /// The counter is required when the credential type is HOTP. For TOTP it's 0.  
        /// </param> 
        /// <param name="requireTouch">
        /// The credential requires the user to touch the key to generate a one-time password.
        /// </param>
        public Credential(string issuer, string account, CredentialType type, HashAlgorithm algorithm, string secret, CredentialPeriod period, int digits, int? counter, bool requireTouch)
        {
            Issuer = issuer;
            AccountName = account;
            Type = type;
            Algorithm = algorithm;
            Secret = secret;
            Period = period;
            Digits = digits;
            Counter = counter;
            RequiresTouch = requireTouch;
        }

        /// <summary>
        /// Checks the name length, which cannot be more than 64bytes.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The credential's name length is invalid.
        /// </exception>
        private void CheckNameLength()
        {
            if (Encoding.UTF8.GetBytes(Name).Length > MaximumNameLength)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidCredentialNameLength);
            }
        }

        /// <summary>
        /// Parses the label string presented as 'period/issuer:account'.
        /// </summary>
        /// <returns>
        /// The triple of extracted period, issuer, and account.
        /// </returns>
        internal static (CredentialPeriod period, string issuer, string account) ParseLabel(string label, CredentialType type)
        {
            CredentialPeriod period = CredentialPeriod.Period30;
            string? issuer = string.Empty;
            string issuerAccount;

            if (type == CredentialType.Totp)
            {
                string[]? parsedLabel = label.Split('/');

                if (parsedLabel.Length > 1)
                {
                    period = (CredentialPeriod)ToInt32(parsedLabel[0], NumberFormatInfo.InvariantInfo);
                    issuerAccount = parsedLabel[1];
                }
                else
                {
                    issuerAccount = parsedLabel[0];
                }
            }
            else
            {
                issuerAccount = label;
                period = CredentialPeriod.Undefined;
            }

            string[]? parsedAccount = issuerAccount.Split(':');

            if (parsedAccount.Length == 2)
            {
                issuer = parsedAccount[0];
            }

            if (parsedAccount.Length > 2)
            {
                issuer = string.Join(":", parsedAccount.Take(parsedAccount.Length - 1));
            }

            string account = parsedAccount.Last();

           return (period, issuer, account);
        }

        /// <summary>
        /// Parses an 'otpauth://' Uri that received from QR reader or manually from server.
        /// </summary>
        /// <remarks>
        /// When you enable two-factor authentication on websites, they usually show you a QR code and ask you to scan and launch an authenticator app.
        /// QR codes are used in scanning secrets to generate one-time passwords. Secrets may be encoded in QR codes as a URI as specified by
        /// https://github.com/google/google-authenticator/wiki/Key-Uri-Format
        /// </remarks>
        /// <returns>
        /// The credential with parameters.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The Uri is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The Uri path or schema is invalid, or the credential's algorithm or period is invalid.
        /// </exception>
        public static Credential ParseUri(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri)); 
            }

            if (!uri.IsAbsoluteUri || uriScheme != uri.Scheme)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidUriScheme);
            }

            string? uriPath = uri.AbsolutePath;
            if (string.IsNullOrWhiteSpace(uriPath))
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidUriPath);
            }

            string uriQuery = uri.Query;
            NameValueCollection? parsedUri = HttpUtility.ParseQueryString(uriQuery);
            
            string defaultIssuer = parsedUri["issuer"];
            (string issuer, string account) = ParseUriPath(uriPath, defaultIssuer);

            string secret = parsedUri["secret"];

            CredentialType type = uri.Host == "totp" ? CredentialType.Totp : CredentialType.Hotp;

            HashAlgorithm algorithm = HashAlgorithm.Sha1;
            string algorithmString = parsedUri["algorithm"];

            if (!string.IsNullOrWhiteSpace(algorithmString))
            {
                string tempAlgorithm = algorithmString.ToUpperInvariant();

                if (tempAlgorithm == "SHA1")
                {
                    algorithm = HashAlgorithm.Sha1;
                }
                else if (tempAlgorithm == "SHA256")
                {
                    algorithm = HashAlgorithm.Sha256;
                }
                else if (tempAlgorithm == "SHA512")
                {
                    algorithm = HashAlgorithm.Sha512;
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.ValueIsNotEnum,
                            tempAlgorithm,
                            nameof(Algorithm)));
                    
                }
            }

            int digits = DefaultDigits;
            string digitsString = parsedUri["digits"];

            if (!string.IsNullOrWhiteSpace(digitsString) && !int.TryParse(digitsString, NumberStyles.Any,
                CultureInfo.InvariantCulture, out digits))
            {
                digits = DefaultDigits;
            }

            CredentialPeriod period = CredentialPeriod.Period30;
            string periodString = parsedUri["period"];

            if (!string.IsNullOrWhiteSpace(periodString))
            {
                if (int.TryParse(periodString, NumberStyles.Any, CultureInfo.InvariantCulture, out int periodInt))
                {
                    period = (CredentialPeriod)periodInt;
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            ExceptionMessages.ValueIsNotEnum,
                            periodString,
                            nameof(CredentialPeriod)));
                }
            }

            string counterString = parsedUri["counter"];
            int? counter = int.TryParse(counterString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : (int?)null;

            return new Credential(Uri.UnescapeDataString(issuer), Uri.UnescapeDataString(account), type, algorithm, secret, period, digits, counter, false);
        }

        /// <summary>
        /// Parses an Uri path.
        /// </summary>
        /// <returns>
        /// The pair of extracted issuer and account.
        /// </returns>
        private static (string issuer, string account) ParseUriPath(string path, string defaultIssuer)
        {
            string tempPath = path.StartsWith("/", true, CultureInfo.InvariantCulture) ? path.Substring(1) : path;

            if (tempPath.Length > MaximumUrlLength)
            {
                tempPath = tempPath.Substring(0, MaximumUrlLength);
            }

            string[]? parsedPath = tempPath.Split(':');

            if (parsedPath.Length > 1)
            {
                return (parsedPath[0], parsedPath[1]);
            }

            return (defaultIssuer, tempPath);
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            hash = (hash * 7) + _issuer?.GetHashCode() ?? "".GetHashCode();
            hash = (hash * 7) + _accountName?.GetHashCode() ?? "".GetHashCode();
            hash = (hash * 7) + _secret?.GetHashCode() ?? "".GetHashCode();
            hash = (hash * 7) + _digits.GetHashCode();
            hash = (hash * 7) + _counter.GetHashCode();
            hash = (hash * 7) + _type.GetHashCode();
            hash = (hash * 7) + _period.GetHashCode();
            hash = (hash * 7) + _algorithm.GetHashCode();

            return hash;
        }

        public override bool Equals(object obj) => Equals(obj as Credential);

        public bool Equals(Credential? credential)
        {
            if (credential is null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, credential))
            {
                return true;
            }

            if (GetType() != credential.GetType())
            {
                return false;
            }

            return (_issuer == credential._issuer) 
                && (_accountName == credential._accountName)
                && (_secret == credential._secret)
                && (_digits == credential._digits)
                && (_counter == credential._counter)
                && (_type == credential._type)
                && (_period == credential._period)
                && (_algorithm == credential._algorithm);
        }

        public static bool operator ==(Credential lhs, Credential rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }

                return false;
            }

            return lhs.Equals(rhs);
        }

        public static bool operator !=(Credential lhs, Credential rhs) => !(lhs == rhs);
    }
}
