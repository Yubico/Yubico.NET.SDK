# Per-method source effect index

All rows await coordinator execution. Full attributes, source bodies, assertions, early returns, helper expansions and reviewed effects are in `hardware-methods.json`. `finally` means local cleanup exists, not complete restoration. RESET retains existing F9.

| Method | Persistent effect | Traits/lane | Local cleanup |
|---|---|---|---|
| PivAuditHardwareReproTests.YESDK1630_PinOnceStateSurvivesConnectionDisposal | reset/write | routine | scope only |
| PivAuditHardwareReproTests.YESDK1632_Ed25519SignsUnmodifiedShortAndLongMessages | reset/write | routine | scope only |
| PivAuthenticationTests.AuthenticateAsync_WithDefaultKey_Succeeds | reset/write | routine | scope only |
| PivAuthenticationTests.AuthenticateAsync_WithWrongKey_ThrowsApduException | reset/write | routine | scope only |
| PivAuthenticationTests.VerifyPinAsync_WithCorrectPin_Succeeds | reset/write | routine | scope only |
| PivAuthenticationTests.VerifyPinAsync_WithWrongPin_ThrowsInvalidPinException | reset/write | routine | scope only |
| PivAuthenticationTests.GetPinAttemptsAsync_ReturnsCorrectCount | reset/write | routine | scope only |
| PivAuthenticationTests.ChangePinAsync_WithCorrectOldPin_Succeeds | reset/write | routine | finally |
| PivCertificateTests.StoreCertificateAsync_GetCertificateAsync_RoundTrip | reset/write | routine | scope only |
| PivCertificateTests.GetCertificateAsync_EmptySlot_ReturnsNull | reset/write | routine | scope only |
| PivCertificateTests.DeleteCertificateAsync_IsIdempotent | reset/write | routine | scope only |
| PivCertificateTests.GetObjectAsync_EmptyObject_ReturnsEmpty | reset/write | routine | scope only |
| PivCompressedCertTests.StoreCertificateAsync_Compressed_RoundTripsCorrectly | reset/write | routine | scope only |
| PivConnectionConcurrencyTests.ConcurrentSignAndObjectRead_OnOneSession_SecondOperationIsRefused | reset/write | routine | scope only |
| PivCryptoTests.SignOrDecryptAsync_EccP256Sign_ProducesValidSignature | reset/write | routine | scope only |
| PivCryptoTests.CalculateSecretAsync_ECDH_ProducesMatchingSharedSecret | reset/write | routine | scope only |
| PivCryptoTests.SignOrDecryptAsync_Ed25519_ProducesSignature | reset/write | routine | scope only |
| PivCryptoTests.SignOrDecryptAsync_EccP384Sign_ProducesValidSignature | reset/write | routine | scope only |
| PivCryptoTests.CalculateSecretAsync_X25519_ProducesSharedSecret | reset/write | routine | scope only |
| PivCryptoTests.SignOrDecryptAsync_Rsa2048Sign_ProducesValidSignature | reset/write | routine | scope only |
| PivCryptoTests.SignOrDecryptAsync_Rsa1024Sign_ProducesValidSignature | reset/write | routine | scope only |
| PivCryptoTests.SignOrDecryptAsync_Rsa3072Sign_ProducesValidSignature | reset/write | Slow routine | scope only |
| PivCryptoTests.SignOrDecryptAsync_Rsa4096Sign_ProducesValidSignature | reset/write | Slow routine | scope only |
| PivCryptoTests.SignOrDecryptAsync_Rsa2048Decrypt_DecryptsCorrectly | reset/write | routine | scope only |
| PivDecryptTests.DecryptAsync_Rsa2048Pkcs1_ReturnsExactPlaintext | reset/write | routine | scope only |
| PivDecryptTests.DecryptAsync_Rsa2048OaepSha256_ReturnsExactPlaintext | reset/write | routine | scope only |
| PivDecryptTests.DecryptAsync_EccSlot_ThrowsArgumentException | reset/write | routine | scope only |
| PivDecryptTests.DecryptAsync_WrongCiphertextLength_ThrowsArgumentException | reset/write | routine | scope only |
| PivDecryptTests.DecryptAsync_Rsa4096Pkcs1_ReturnsExactPlaintext | reset/write | Slow routine | scope only |
| PivDiscoveryContentionTests.FindAllAsync_WhileCardBusyWithRsa4096Keygen_CompletesWithoutWaitingForKeygen | reset/write | Slow routine | finally |
| PivDiscoveryContentionTests.FindAllAsync_WhilePivSessionHasVerifiedPin_DoesNotClobberSessionState | reset/write | routine | scope only |
| PivFullWorkflowTests.CompleteWorkflow_GenerateKeySignVerify | reset/write | routine | scope only |
| PivFullWorkflowTests.CompleteWorkflow_ECDHKeyAgreement | reset/write | routine | scope only |
| PivFullWorkflowTests.CompleteWorkflow_MoveKeyBetweenSlots | reset/write | routine | scope only |
| PivFullWorkflowTests.CompleteWorkflow_AttestGeneratedKey | reset/write | routine | scope only |
| PivHotplugContentionTests.PivSession_KeyRemovedMidSession_FailsBoundedAndDoesNotStrandTheCcidLease | read/session state | Slow Presence | finally |
| PivImportTests.ImportKeyAsync_Rsa2048_CanSignAndVerify | reset/write | routine | scope only |
| PivImportTests.ImportKeyAsync_Rsa3072_MetadataMatchesImportedKey | reset/write | Slow routine | scope only |
| PivImportTests.ImportKeyAsync_Ed25519_CanSignAndVerify | reset/write | routine | finally |
| PivImportTests.ImportKeyAsync_X25519_MetadataMatchesImportedKey | reset/write | routine | finally |
| PivKeyOperationsTests.GenerateKeyAsync_EccP256_ReturnsPublicKey | reset/write | routine | scope only |
| PivKeyOperationsTests.GenerateKeyAsync_Ed25519_ReturnsPublicKey | reset/write | routine | scope only |
| PivKeyOperationsTests.AttestKeyAsync_GeneratedKey_ReturnsCertificate | reset/write | routine | scope only |
| PivKeyOperationsTests.MoveKeyAsync_MovesToNewSlot_KeyRemainsFunctional | reset/write | routine | scope only |
| PivKeyOperationsTests.ImportKeyAsync_EccP256_CanSign | reset/write | routine | scope only |
| PivKeyOperationsTests.DeleteKeyAsync_RemovesKey_SlotBecomesEmpty | reset/write | routine | scope only |
| PivKeyOperationsTests.PutObjectAsync_GetObjectAsync_RoundTrip | reset/write | routine | finally |
| PivKeyOperationsTests.GetSerialNumberAsync_ReturnsDeviceSerial | read/session state | routine | scope only |
| PivManagementKeyTests.SetManagementKeyAsync_ChangesToNewKey | reset/write | routine | finally |
| PivManagementKeyTests.SetManagementKeyAsync_AES256_Succeeds | reset/write | routine | finally |
| PivManagementKeyTests.SetManagementKeyAsync_AES128_Succeeds | reset/write | routine | finally |
| PivManagementKeyTests.SetManagementKeyAsync_SameSessionStateTracksSuccessfulSetAndReset | reset/write | routine | finally |
| PivMetadataTests.GetPinMetadataAsync_ReturnsValidMetadata | reset/write | routine | scope only |
| PivMetadataTests.GetSlotMetadataAsync_EmptySlot_ReturnsNull | reset/write | routine | scope only |
| PivMetadataTests.GetSlotMetadataAsync_WithKey_ReturnsMetadata | reset/write | routine | scope only |
| PivMetadataTests.GetManagementKeyMetadataAsync_ReturnsValidMetadata | reset/write | routine | scope only |
| PivMetadataTests.GetBioMetadataAsync_NonBioDevice_ThrowsOrReturnsError | read/session state | routine | scope only |
| PivMultiKeyContentionTests.FindAllAsync_WithOpenSessionOnOneKey_IdentifiesOtherKeysAndPreservesSession | reset/write | routine | scope only |
| PivMultiKeyContentionTests.ConcurrentPivSessions_OnTwoKeys_OperateIndependently | reset/write | routine | scope only |
| PivMultiKeyContentionTests.Rsa4096Keygen_OnOneKey_DoesNotDelayPivOperationsOnAnotherKey | reset/write | Slow routine | scope only |
| PivPinOnlyWorkflowTests.PinProtectedMode_ReopensRecoversAndDisablesWithDefaultManagementKey | reset/write | routine | finally |
| PivPinRetryTests.PinRetries_DecrementAfterEachFailedAttempt_MetadataMatches | reset/write | routine | finally |
| PivPinRetryTests.SetPinAttempts_RequiresPin_AndResetsPinAndPukEvenWithUnchangedLimits | reset/write | routine | finally |
| PivPinRetryTests.SetPinAttempts_CustomLimit_EnforcedDuringFailedAttempts | reset/write | routine | finally |
| PivPinRetryTests.PinMetadata_IsDefault_ReflectsChanges | reset/write | routine | finally |
| PivPolicyTests.GenerateKey_PinPolicyNever_CanSignWithoutPin | reset/write | routine | scope only |
| PivPolicyTests.GenerateKey_PinPolicyAlways_RequiresPinForEachSign | reset/write | routine | scope only |
| PivPukTests.ChangePukAsync_WithCorrectOldPuk_Succeeds | reset/write | routine | finally |
| PivPukTests.UnblockPinAsync_AfterBlockedPin_RestoresAccess | reset/write | routine | finally |
| PivPukTests.GetPukMetadataAsync_ReturnsValidMetadata | reset/write | routine | scope only |
| PivPukTests.SetPinAttemptsAsync_CustomLimit_EnforcesLimit | reset/write | routine | finally |
| PivResetTests.ResetAsync_RestoresToDefaults | reset/write | routine | scope only |
| PivResetTests.ResetAsync_PinMetadataShowsDefault | reset/write | routine | scope only |
| PivResetTests.ResetAsync_ClearsAllSlots | reset/write | routine | scope only |
| PivSessionContentionTests.GetDeviceInfoAsync_WhilePivSessionHasVerifiedPin_IsRefusedWithoutClobberingState | reset/write | routine | scope only |
| PivSessionContentionTests.CreateManagementSessionAsync_WhilePivHoldsCcid_IsRefusedWithoutFallback | reset/write | routine | scope only |
| PivSessionContentionTests.ConnectAsync_SecondSmartCardConnection_WhilePivSessionOpen_IsRefused | reset/write | routine | scope only |
| PivSessionContentionTests.SuccessiveSessions_OverOneCallerOwnedConnection_BothReachTheCard | read/session state | routine | scope only |
| PivSessionContentionTests.SecondSession_OnOneLiveConnection_IsRefused | read/session state | routine | scope only |
| PivSigningAlgorithmTests.SignRsa2048_WithSha1Hash_ProducesValidSignature | reset/write | routine | scope only |
| PivSigningAlgorithmTests.SignRsa2048_WithSha384Hash_ProducesValidSignature | reset/write | routine | scope only |
| PivSigningAlgorithmTests.SignRsa2048_WithSha512Hash_ProducesValidSignature | reset/write | routine | scope only |
| PivSigningAlgorithmTests.DecryptRsa2048_OaepSha1_ReturnsExactPlaintext | reset/write | routine | scope only |
| PivSigningAlgorithmTests.SignEccP256_WithSha256Hash_ProducesValidSignature | reset/write | routine | scope only |
| PivSigningAlgorithmTests.SignEccP384_WithSha384Hash_ProducesValidSignature | reset/write | routine | scope only |
| PivSlotOverwriteTests.GenerateKey_RsaThenEcc_OverwritesWithEcc | reset/write | routine | scope only |
| PivSlotOverwriteTests.GenerateKey_EccThenRsa_OverwritesWithRsa | reset/write | routine | scope only |
| UserPresenceProbeTests.InspectPrerequisites_ReadOnly_PrintsNonSecretState | read/session state | Presence | scope only |
| UserPresenceProbeTests.SignOrDecryptAsync_TouchPolicyAlways_CompletesOnceAndSessionRemainsUsable | reset/write | Slow Presence | finally |
