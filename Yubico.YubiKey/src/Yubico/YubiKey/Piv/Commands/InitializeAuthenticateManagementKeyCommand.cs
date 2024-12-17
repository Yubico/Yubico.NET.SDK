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
using Yubico.Core.Iso7816;

namespace Yubico.YubiKey.Piv.Commands
{
    /// <summary>
    /// Initiate the process to authenticate the PIV management key.
    /// </summary>
    /// <remarks>
    /// In the PIV standard, there is a command called GENERAL AUTHENTICATE.
    /// Although it is one command, it can do four things: authenticate a
    /// management key (challenge-response), sign arbitrary data, RSA decryption,
    /// and EC Diffie-Hellman. The SDK breaks these four operations into separate
    /// classes. This class is how you initialize the process of performing
    /// "GENERAL AUTHENTICATE: management key".
    /// <para>
    /// The partner Response class is <see cref="InitializeAuthenticateManagementKeyResponse"/>.
    /// </para>
    /// <para>
    /// Some operations require the management key. See the User's Manual entry
    /// on <xref href="UsersManualPinPukMgmtKey"> the management key</xref>. Use
    /// this command to begin the process of authenticating the key for such
    /// operations. The User's Manual entry on
    /// <xref href="UsersManualPivAccessControl"> access control</xref> has more
    /// information on how to use this authentication.
    /// </para>
    /// <para>
    /// Upon manufacture of a YubiKey, the management key is either a Triple-DES key (firmware prior to 5.7) or an AES-192 key (firmware 5.7 and later), and
    /// it starts out as a default value:
    /// </para>
    /// <code>
    /// (hex) 0102030405060708 0102030405060708 0102030405060708
    /// </code>
    /// <para>
    /// You will likely change it using the <see cref="SetManagementKeyCommand"/>.
    /// </para>
    /// <para>
    /// Starting with YubiKey 5.4.2, it is possible to use an AES key as the
    /// management key. An AES key can be 128, 192, or 256 bits (16, 24, and 32
    /// bytes respectively).
    /// </para>
    /// <para>
    /// Authentication is a challenge-response process involving two or three
    /// steps. The off-card application sends in the Command APDUs and the
    /// YubiKey returns the Response APDUs. Note that both the term
    /// "challenge-response" and the APDU pair "Command and Response" use the
    /// term "response". To avoid confusion, when discussing the process, there
    /// will be the "Command APDU" and "Response APDU", the "Command Class" and
    /// the "Response Class", the "Command Object" and the "Response Object".
    /// Then there will be "Challenge" and "Response".
    /// </para>
    /// <para>
    /// There are two versions of the process: (1) authenticate the off-card
    /// application to the YubiKey only, or (2) mutual authentication, where the
    /// YubiKey is authenticated to the off-card application as well.
    /// </para>
    /// <para>
    /// <u>Single Authentication</u>
    /// <list type="table">
    /// <item>
    ///  <term><b>Off-Card Application</b></term>
    ///  <description><b>YubiKey</b></description>
    /// </item>
    /// <item>
    ///  <term><i>Step 1</i></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term><b>Command APDU</b><br/> initiates the Process</term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term></term>
    ///  <description>Generate random <c>Client Authentication Challenge</c></description>
    /// </item>
    /// <item>
    ///  <term></term>
    ///  <description><b>Response APDU</b><br/> contains <c>Client Authentication Challenge</c></description>
    /// </item>
    /// <item>
    ///  <term><i>Step 2</i></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term>Compute <c>Client Authentication Response</c> based on <c>Client Authentication Challenge</c></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term><b>Command APDU</b><br/> contains <c>Client Authentication Response</c></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term></term>
    ///  <description>Verify <c>Client Authentication Response</c></description>
    /// </item>
    /// <item>
    ///  <term></term>
    ///  <description><b>Response APDU</b><br/> contains no data<br/>Status is either
    ///  Success or AuthenticationRequired</description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// <u>Mutual Authentication</u>
    /// <list type="table">
    /// <item>
    ///  <term><b>Off-Card Application</b></term>
    ///  <description><b>YubiKey</b></description>
    /// </item>
    /// <item>
    ///  <term><i>Step 1</i></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term><b>Command APDU</b><br/> initiates the Process</term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term></term>
    ///  <description>Generate random <c>Client Authentication Challenge</c></description>
    /// </item>
    /// <item>
    ///  <term></term>
    ///  <description><b>Response APDU</b><br/> contains <c>Client Authentication Challenge</c></description>
    /// </item>
    /// <item>
    ///  <term><i>Step 2</i></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term>Compute <c>Client Authentication Response</c> based on <c>Client Authentication Challenge</c></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term>Generate random <c>YubiKey Authentication Challenge</c></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term><b>Command APDU</b><br/> contains <c>Client Authentication Response</c> and <c>YubiKey Authentication Challenge</c></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term></term>
    ///  <description>Verify <c>Client Authentication Response</c></description>
    /// </item>
    /// <item>
    ///  <term></term>
    ///  <description>Compute <c>YubiKey Authentication Response</c> based on <c>YubiKey Authentication Challenge</c></description>
    /// </item>
    /// <item>
    ///  <term></term>
    ///  <description><b>Response APDU</b><br/>Status is either Success or
    ///  AuthenticationRequired<br/>if Success, Response APDU contains <c>YubiKey Authentication Response</c></description>
    /// </item>
    /// <item>
    ///  <term><i>Step 3</i></term>
    ///  <description></description>
    /// </item>
    /// <item>
    ///  <term>Verify <c>YubiKey Authentication Response</c></term>
    ///  <description></description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// Each response is built from the challenge and the management key. If both
    /// parties possess the same management key, the response will match what the
    /// challenger expects.
    /// </para>
    /// <para>
    /// Note that the PIV standard has three elements: "witness", "challenge",
    /// and "response". In mutual authentication, the YubiKey sends a "witness",
    /// which the application decrypts to generate the response. In single
    /// authentication, the YubiKey sends a "challenge" and the application
    /// encrypts it to generate the response. For the purposes of this
    /// documentation, the "witness-response" and "challenge-response" are the
    /// same concept, namely a "challenge-response".
    /// </para>
    /// <para>
    /// To perform step 1, build an <c>InitializeAuthenticateManagementKeyCommand</c> object,
    /// specifying single or mutual authentication, and send it to the YubiKey
    /// using the Connection class's <c>SendCommand</c> method.
    /// </para>
    /// <para>
    /// After the <c>SendCommand</c> completes, check the Response Object to
    /// verify it worked. To do so, look at the <c>Status</c> property in the
    /// Response Object, it will be <c>Success</c>. The return from
    /// <c>GetData</c> will be a <c>ReadOnlyMemory</c> object containing the
    /// challenge from the YubiKey (this is "Client Authentication Challenge").
    /// It is likely you will never need to examine the challenge, the Command
    /// object in Step 2 will process it.
    /// </para>
    /// <para>
    /// To perform step 2, create a new instance of the
    /// <c>CompleteAuthenticateManagementKeyCommand</c> class, supplying the
    /// previous Response Object, along with the management key. Now call
    /// <c>connection.SendCommand</c> again. Under the covers, "Client
    /// Authentication Response" (the response to "Client Authentication
    /// Challenge") is computed and both it and "YubiKey Authentication
    /// Challenge" are sent to the YubiKey. If this is single authentication,
    /// only "Client Authentication Response" is sent.
    /// </para>
    /// <para>
    /// Examine the resulting Response Object. If the <c>Status</c> property is
    /// <c>Success</c>, you have authenticated the management key. If the
    /// <c>Status</c> property is <c>AuthenticationRequired</c>, the management
    /// key is not authenticated (either the off board app or the YubiKey did not
    /// authenticate). If <c>Status</c> is not one of those values, then some other error occurred and
    /// the authentication process was not completed. In this situation the Response's
    /// <c>GetData</c> method will throw an exception if called.
    /// </para>
    /// <para>
    /// If the process is mutual authentication, and the response object's
    /// <c>Status</c> is <c>AuthenticationRequired</c>, you will likely want to
    /// know whether the YubiKey was also authenticated. Call the response
    /// object's <c>GetData</c> method, the return will be an
    /// <see cref="AuthenticateManagementKeyResult"/> enum.
    /// </para>
    /// <para>
    /// Note that if, during mutual authentication, the YubiKey does not
    /// authenticate the management key, it will not be able to even attempt to
    /// authenticate the YubiKey, so its authentication status will be "Unknown".
    /// </para>
    /// <para>
    /// Note also that if the YubiKey does not authenticate the management key,
    /// calling <c>GetData</c> will not throw an exception. The operation
    /// determines whether the management key authenticates or not. If it does
    /// not, then the YubiKey has completed the operation successfully. That is,
    /// the operation was performed to completion, it's just that the operation
    /// determined that the management key was not correct.
    /// </para>
    /// <para>
    /// Example:
    /// </para>
    /// <code language="csharp">
    ///   /* This example assumes the application has a method to collect a
    ///    * management key.
    ///    */
    ///   byte[] mgmtKey;<br/>
    ///   IYubiKeyConnection connection = key.Connect(YubiKeyApplication.Piv);<br/>
    ///   var initAuthMgmtKeyCommand =
    ///       new InitializeAuthenticateManagementKeyCommand(true, PivAlgorithm.Aes192);
    ///   InitializeAuthenticateManagementKeyResponse initAuthMgmtKeyResponse =
    ///       connection.SendCommand(initAuthMgmtKeyCommand);<br/>
    ///   if (initAuthMgmtKeyResponse != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    ///   mgmtKey = CollectMgmtKey();
    ///   var completeAuthMgmtKeyCommand = new CompleteAuthenticateManagementKeyCommand(
    ///       initAuthMgmtKeyResponse, mgmtKey, PivAlgorithm.Aes192);
    ///   CompleteAuthenticateManagementKeyResponse completeAuthMgmtKeyResponse =
    ///       connection.SendCommand(completeAuthMgmtKeyCommand);<br/>
    ///   if (completeAuthMgmtKeyResponse.Status == ResponseStatus.AuthenticationRequired)
    ///   {
    ///       AuthenticateManagementKeyResult authResult = completeAuthMgmtKeyResponse.GetData();
    ///       /* The value of authResult will be either MutualOffCardAuthenticationFailed
    ///        * or MutualYubiKeyAuthenticationFailed, to indicate why the
    ///        * authentication failed. */
    ///   }
    ///   else if (completeAuthMgmtKeyResponse.Status != ResponseStatus.Success)
    ///   {
    ///     // Handle error
    ///   }
    ///
    ///   /* Continue with operations that needed the mgmt key authenticated. */
    ///
    ///   CryptographicOperations.ZeroMemory(mgmtKey);
    /// </code>
    /// </remarks>
    public sealed class InitializeAuthenticateManagementKeyCommand : IYubiKeyCommand<InitializeAuthenticateManagementKeyResponse>
    {
        private const byte AuthMgmtKeyInstruction = 0x87;
        private const byte AuthMgmtKeyParameter2 = 0x9B;

        private const int Step1Length = 4;
        private const int T2Offset = 2;
        private const byte Step1T2MutualAuth = 0x80;

        private readonly byte[] _data;

        /// <summary>
        /// Which algorithm is the management key.
        /// </summary>
        public PivAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Gets the YubiKeyApplication to which this command belongs. For this
        /// command it's PIV.
        /// </summary>
        /// <value>
        /// YubiKeyApplication.Piv
        /// </value>
        public YubiKeyApplication Application => YubiKeyApplication.Piv;


        [Obsolete("This constructor is deprecated. Users must specify management key algorithm type, as it cannot be assumed.")]
        public InitializeAuthenticateManagementKeyCommand()
            : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InitializeAuthenticateManagementKeyCommand class for
        /// Mutual Authentication.
        /// </summary>
        /// <remarks>
        /// Using this constructor is equivalent to
        /// <code language="csharp">
        ///  new InitializeAuthenticateManagementKeyCommand(true, PivAlgorithm.AES192);
        /// </code>
        /// </remarks>
        public InitializeAuthenticateManagementKeyCommand(PivAlgorithm algorithm)
        : this(true, algorithm)
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// InitializeAuthenticateManagementKeyCommand class for the specified
        /// mutual or single authentication, and a Triple-DES management key.
        /// </summary>
        /// <remarks>
        /// This will build a Command object that can initiate the authentication
        /// process. If you want to initiate for mutual authentication, pass in
        /// <c>true</c> for the <c>mutualAuthentication</c> argument. To initiate
        /// single authentication, pass in <c>false</c>.
        /// </remarks>
        /// <param name="mutualAuthentication">
        /// <c>True</c> for mutual authentication, <c>false</c> for single.
        /// </param>
        [Obsolete("This constructor is deprecated. Users must specify management key algorithm type, as it cannot be assumed.")]
        public InitializeAuthenticateManagementKeyCommand(bool mutualAuthentication)
            : this(mutualAuthentication, PivAlgorithm.TripleDes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the
        /// InitializeAuthenticateManagementKeyCommand class for the specified
        /// mutual or single authentication, and a management key of the
        /// specified algorithm.
        /// </summary>
        /// <remarks>
        /// This will build a Command object that can initiate the authentication
        /// process. If you want to initiate for mutual authentication, pass in
        /// <c>true</c> for the <c>mutualAuthentication</c> argument. To initiate
        /// single authentication, pass in <c>false</c>.
        /// <para>
        /// Note that you must know the management key's algorithm before calling
        /// this constructor. If the YubiKey's version is before 5.4.2, the
        /// algorithm is <c>TripleDes</c>. For 5.4.2 and later YubiKeys, the
        /// algorithm can be ascertained by getting the metadata.
        /// </para>
        /// </remarks>
        /// <param name="mutualAuthentication">
        /// <c>True</c> for mutual authentication, <c>false</c> for single.
        /// </param>
        /// <param name="algorithm">
        /// The algorithm of the management key. It must be <c>TripleDes</c>,
        /// <c>Aes128</c>, <c>Aes192</c>, or <c>Aes256</c>,
        /// </param>
        public InitializeAuthenticateManagementKeyCommand(bool mutualAuthentication, PivAlgorithm algorithm)
        {
            Algorithm = algorithm;

            // The step 1 mutual auth data is this
            // Total len =  4: 7C 02 80 00
            //
            // The step 1 single auth data is this
            // Total len =  4: 7C 02 81 00
            _data = new byte[Step1Length] {
                0x7C, 0x02, 0x81, 0x00
            };

            if (mutualAuthentication)
            {
                _data[T2Offset] = Step1T2MutualAuth;
            }
        }

        /// <inheritdoc />
        public CommandApdu CreateCommandApdu() => new CommandApdu
        {
            Ins = AuthMgmtKeyInstruction,
            P1 = (byte)Algorithm,
            P2 = AuthMgmtKeyParameter2,
            Data = _data,
        };

        /// <inheritdoc />
        public InitializeAuthenticateManagementKeyResponse CreateResponseForApdu(ResponseApdu responseApdu) =>
          new InitializeAuthenticateManagementKeyResponse(responseApdu, Algorithm);
    }
}
