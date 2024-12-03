using System;
using Moq;
using Xunit;
using Yubico.Core.Iso7816;
using Yubico.YubiKey.Cryptography;
using Yubico.YubiKey.Scp.Commands;
using Yubico.YubiKey.Scp.Helpers;

namespace Yubico.YubiKey.Scp
{
    public class Scp03StateTests
    {
        readonly byte[] ResponseData;
        
        internal Scp03State State { get; set; }
        
        public Scp03StateTests()
        {
            var parent = new Mock<IApduTransform>();
            var keyParams = Scp03KeyParameters.DefaultKey;
            var hostChallenge = new byte[8];
            var cardChallenge = new byte[8];

            ResponseData = GetFakeResponseApduData(hostChallenge, cardChallenge);

            parent.Setup(p => p.Invoke(
                    It.IsAny<CommandApdu>(),
                    typeof(InitializeUpdateCommand),
                    typeof(InitializeUpdateResponse)))
                .Returns(new ResponseApdu(ResponseData));
            
            
            parent.Setup(p => p.Invoke(
                    It.IsAny<CommandApdu>(),
                    typeof(ExternalAuthenticateCommand),
                    typeof(ExternalAuthenticateResponse)))
                .Returns(new ResponseApdu(ResponseData));

            // Act
            State = Scp03State.CreateScpState(parent.Object, keyParams, hostChallenge);
        }

        [Fact]
        public void CreateScpState_ValidParameters_InitializesCorrectly()
        {
            // Arrange
            var parent = new Mock<IApduTransform>();
            var keyParams = Scp03KeyParameters.DefaultKey;
            var hostChallenge = new byte[8];
            var cardChallenge = new byte[8];

            var responseApduData = GetFakeResponseApduData(hostChallenge, cardChallenge);

            parent.Setup(p => p.Invoke(
                    It.IsAny<CommandApdu>(),
                    typeof(InitializeUpdateCommand),
                    typeof(InitializeUpdateResponse)))
                .Returns(new ResponseApdu(responseApduData));
            
            
            parent.Setup(p => p.Invoke(
                    It.IsAny<CommandApdu>(),
                    typeof(ExternalAuthenticateCommand),
                    typeof(ExternalAuthenticateResponse)))
                .Returns(new ResponseApdu(responseApduData));

            // Act
            var state = Scp03State.CreateScpState(parent.Object, keyParams, hostChallenge);

            // Assert
            Assert.NotNull(state);
            Assert.NotNull(state.GetDataEncryptor());
        }

        [Fact]
        public void CreateScpState_NullPipeline_ThrowsArgumentNullException()
        {
            // Arrange
            var keyParams = Scp03KeyParameters.DefaultKey;
            byte[] hostChallenge = new byte[8];

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                Scp03State.CreateScpState(null!, keyParams, hostChallenge));
        }

        [Fact]
        public void CreateScpState_NullKeyParams_ThrowsArgumentNullException()
        {
            // Arrange
            var pipeline = new Mock<IApduTransform>();
            byte[] hostChallenge = new byte[8];

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                Scp03State.CreateScpState(pipeline.Object, null!, hostChallenge));
        }

        [Fact]
        public void EncodeCommand_ValidCommand_ReturnsEncodedApdu()
        {
            // Arrange
            using var rng = CryptographyProviders.RngCreator();
            Span<byte> putKeyData = stackalloc byte[256];
            rng.GetBytes(putKeyData);
            var originalCommand = new PutKeyCommand(1, 2, putKeyData.ToArray()).CreateCommandApdu();
        
            // Act
            var encodedCommand = State.EncodeCommand(originalCommand);
        
            // Assert
            Assert.NotNull(encodedCommand);
            Assert.NotEqual(originalCommand.Data, encodedCommand.Data);
        }
       
        private static byte[] GetFakeResponseApduData(
            byte[] hostChallenge,
            byte[] cardChallenge)
        {
            Array.Fill(hostChallenge, (byte)1);
            Array.Fill(cardChallenge, (byte)1);

            // Derive session keys
            var sessionKeys = Derivation.DeriveSessionKeysFromStaticKeys(
                Scp03KeyParameters.DefaultKey.StaticKeys,
                hostChallenge,
                cardChallenge);

            // Check supplied card cryptogram
            var calculatedCardCryptogram = Derivation.DeriveCryptogram(
                Derivation.DDC_CARD_CRYPTOGRAM,
                sessionKeys.MacKey.Span,
                hostChallenge,
                cardChallenge);

            var responseApduData = new byte[31];
            Array.Fill(responseApduData, (byte)1);
            responseApduData[^2] = 0x90;
            responseApduData[^1] = 0x00;

            // Add fake Card Crypto response
            calculatedCardCryptogram.Span.CopyTo(responseApduData.AsSpan(21..29));
            return responseApduData;
        }
    }
}
