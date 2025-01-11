using System.Security.Cryptography;
using Xunit;

namespace Yubico.YubiKey.Cryptography
{
    public class ECParametersExtensionsTests
    {
        [Fact]
        public void DeepCopy_WithValidParameters_CreatesIndependentCopy()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var original = ecdsa.ExportParameters(true);

            // Act
            var copy = original.DeepCopy();

            // Assert
            Assert.Equal(original.Curve.Oid.Value, copy.Curve.Oid.Value);
            Assert.Equal(original.Q.X, copy.Q.X);
            Assert.Equal(original.Q.Y, copy.Q.Y);
            Assert.Equal(original.D, copy.D);
            
            // Verify independence
            Assert.NotSame(original.Q.X, copy.Q.X);
            Assert.NotSame(original.Q.Y, copy.Q.Y);
            Assert.NotSame(original.D, copy.D);
        }

        [Fact]
        public void DeepCopy_WithNullArrays_HandlesNullsCorrectly()
        {
            // Arrange
            var original = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = null,
                    Y = null
                },
                D = null
            };

            // Act
            var copy = original.DeepCopy();

            // Assert
            Assert.Equal(original.Curve.Oid.Value, copy.Curve.Oid.Value);
            Assert.Null(copy.Q.X);
            Assert.Null(copy.Q.Y);
            Assert.Null(copy.D);
        }

        [Fact]
        public void DeepCopy_ModifyingCopyDoesNotAffectOriginal()
        {
            // Arrange
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var original = ecdsa.ExportParameters(true);
            var copy = original.DeepCopy();
            Assert.NotNull(copy.Q.X);

            // Act
            copy.Q.X[0] = (byte)(copy.Q.X[0] + 1);

            // Assert
            Assert.NotNull(original.Q.X);
            Assert.NotEqual(original.Q.X[0], copy.Q.X[0]);
        }
    }
}
