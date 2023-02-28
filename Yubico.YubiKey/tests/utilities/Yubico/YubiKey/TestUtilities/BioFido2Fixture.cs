// Copyright 2023 Yubico AB
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
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;
using Yubico.YubiKey.Fido2;
using Yubico.YubiKey.Fido2.Commands;
using Yubico.YubiKey.Cryptography;

namespace Yubico.YubiKey.TestUtilities
{
    public class BioFido2Fixture : SimpleIntegrationTestConnection
    {
        private bool _disposed;
        private readonly Fido2ResetForTest _resetObj;
        private readonly List<RpInfo> _rpInfoList;
        private int _counter;
        private readonly SHA256 _digester;

        public Func<KeyEntryData, bool> KeyCollector { get; private set; }

        // The list of Rps and the user identities for which credentials have
        // been made.
        public IReadOnlyList<RpInfo> RpInfoList => _rpInfoList;

        public bool HasCredentials { get; private set; }

        // Find the YubkKey Bio, reset it, then set the PIN to "123456"
        public BioFido2Fixture()
            : base(YubiKeyApplication.Fido2, StandardTestDevice.Bio)
        {
            var newPin = new ReadOnlyMemory<byte>(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 });
            _resetObj = new Fido2ResetForTest(SerialNumber, newPin);
            if (_resetObj.RunFido2Reset() != ResponseStatus.Success)
            {
                throw new InvalidOperationException("Could not Reset the YubiKey");
            }
            KeyCollector = _resetObj.KeyCollector;
            HasCredentials = false;

            using RandomNumberGenerator randomObject = CryptographyProviders.RngCreator();
            _counter = randomObject.GetInt32(0x000000001, 0x00010000);
            _rpInfoList = new List<RpInfo>(5);
            _digester = CryptographyProviders.Sha256Creator();
        }

        // Add credentials for an Rp.
        // This method will create a "random" relying party, then make the number
        // of credentials requested. It will then add the info to the RpInfoList.
        // That is, the YubiKey will have the new credentials, and you can look
        // at the RpInfoList to get information about what was stored.
        public void AddCredentials(int discoverableCount, int nonDiscoverableCount)
        {
            // Create a "random" Rp
            var rp = new RpInfo(_digester, GetCounter());

            using (var fido2Session = new Fido2Session(Device))
            {
                fido2Session.KeyCollector = KeyCollector;
                for (int index = 0; index < discoverableCount; index++)
                {
                    rp.AddUser(fido2Session, _digester, GetCounter(), true);
                }

                for (int index = 0; index < nonDiscoverableCount; index++)
                {
                    rp.AddUser(fido2Session, _digester, GetCounter(), false);
                }
            }

            _rpInfoList.Add(rp);

            HasCredentials = true;
        }

        // Find the RpInfo for the given relyingParty.
        // If there is no match, throw an exception.
        public RpInfo MatchRelyingParty(RelyingParty relyingParty)
        {
            for (int index = 0; index < _rpInfoList.Count; index++)
            {
                if (_rpInfoList[index].RelyingParty.Id.Equals(relyingParty.Id))
                {
                    return _rpInfoList[index];
                }
            }

            throw new InvalidOperationException("No matching RP found.");
        }

        // Get the UserEntity/MakeCredentialData pair out of the RpInfoList that
        // matches the given RP and User.
        public Tuple<UserEntity,MakeCredentialData> MatchUser(RelyingParty relyingParty, UserEntity user)
        {
            RpInfo rpInfo = MatchRelyingParty(relyingParty);
            UserEntity[] userArray = rpInfo.Users.Keys.ToArray<UserEntity>();
            for (int index = 0; index < userArray.Length; index++)
            {
                bool isMatch = MemoryExtensions.SequenceEqual<byte>(user.Id.Span, userArray[index].Id.Span);
                if (isMatch)
                {
                    return Tuple.Create(userArray[index], rpInfo.Users[userArray[index]]);
                }
            }

            throw new InvalidOperationException("No matching user found.");
        }

        private int GetCounter()
        {
            _counter++;
            _counter &= 0x0000ffff;
            return _counter;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _digester.Dispose();
                }

                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }

    public sealed class RpInfo
    {
        private readonly Dictionary<UserEntity, MakeCredentialData> _users;
        private int _totalCount;

        public RelyingParty RelyingParty { get; private set; }

        public ReadOnlyMemory<byte> ClientDataHash { get; private set; }

        public ReadOnlyMemory<byte> RelyingPartyIdHash { get; private set; }

        // This is a list of user/makeCredentialData pairs.
        public IReadOnlyDictionary<UserEntity, MakeCredentialData> Users => _users;

        public int DiscoverableCount { get; private set; }

        public int NonDiscoverableCount => _totalCount - DiscoverableCount;

        public RpInfo(SHA256 digester, int counter)
        {
            string rpId = "rp" + counter.ToString("X4", CultureInfo.InvariantCulture) + ".com";
            string rpName = "RelyingParty" + counter.ToString("D", CultureInfo.InvariantCulture);

            RelyingParty = new RelyingParty(rpId)
            {
                Name = rpName,
            };

            _users = new Dictionary<UserEntity, MakeCredentialData>(5);

            digester.Initialize();
            // This is not the real ClientDataHash, but for this test code, it
            // will work.
            byte[] digest = digester.ComputeHash(Encoding.UTF8.GetBytes(rpName));
            ClientDataHash = new ReadOnlyMemory<byte>(digest);

            digester.Initialize();
            byte[] idDigest = digester.ComputeHash(Encoding.UTF8.GetBytes(rpId));
            RelyingPartyIdHash = new ReadOnlyMemory<byte>(idDigest);
        }

        public void AddUser(Fido2Session fido2Session, SHA256 digester, int counter, bool isDiscoverable)
        {
            string userName = "user" + counter.ToString("X4", CultureInfo.InvariantCulture);
            string displayName = "User " + counter.ToString("D", CultureInfo.InvariantCulture);
            byte[] nameBytes = Encoding.UTF8.GetBytes(userName);
            digester.Initialize();
            byte[] digest = digester.ComputeHash(nameBytes);
            digest[0] = 0;
            digest[0] = isDiscoverable ? (byte)1 : (byte)0;

            var userEntity = new UserEntity(new ReadOnlyMemory<byte>(digest))
            {
                Name = userName,
                DisplayName = displayName,
            };

            var makeParams = new MakeCredentialParameters(RelyingParty, userEntity)
            {
                ClientDataHash = ClientDataHash
            };
            makeParams.AddExtension("largeBlobKey", new byte[] { 0xF5 });
            makeParams.AddCredBlobExtension(nameBytes, fido2Session.AuthenticatorInfo);

            _totalCount++;
            if (isDiscoverable)
            {
                makeParams.AddOption(AuthenticatorOptions.rk, true);
                DiscoverableCount++;
            }

            fido2Session.VerifyPin(PinUvAuthTokenPermissions.MakeCredential, RelyingParty.Id);
            MakeCredentialData mcData = fido2Session.MakeCredential(makeParams);

            _users.Add(userEntity, mcData);
        }
    }
}
