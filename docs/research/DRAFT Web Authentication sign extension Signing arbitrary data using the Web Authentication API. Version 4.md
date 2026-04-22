---
title: "DRAFT: Web Authentication sign extension: Signing arbitrary data using the Web Authentication API. Version 4"
source: "https://yubicolabs.github.io/webauthn-sign-extension/4/#sctn-sign-extension"
author:
  - "[[Editor’s Draft]]"
published: 2025-08-26
created: 2026-04-22
description:
tags:
  - "clippings"
---
## 1\. Introduction

*This section is not normative.*

This specification defines an API enabling the creation and use of strong, attested, [scoped](#scope), public key-based credentials by [web applications](#web-application), for the purpose of strongly authenticating users. A [public key credential](#public-key-credential) is created and stored by a *[WebAuthn Authenticator](#webauthn-authenticator)* at the behest of a *[WebAuthn Relying Party](#webauthn-relying-party)*, subject to . Subsequently, the [public key credential](#public-key-credential) can only be accessed by [origins](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) belonging to that [Relying Party](#relying-party). This scoping is enforced jointly by *[conforming User Agents](#conforming-user-agent)* and *[authenticators](#authenticator)*. Additionally, privacy across [Relying Parties](#relying-party) is maintained; [Relying Parties](#relying-party) are not able to detect any properties, or even the existence, of credentials [scoped](#scope) to other [Relying Parties](#relying-party).

[Relying Parties](#relying-party) employ the [Web Authentication API](#web-authentication-api) during two distinct, but related, [ceremonies](#ceremony) involving a user. The first is [Registration](#registration), where a [public key credential](#public-key-credential) is created on an [authenticator](#authenticator), and [scoped](#scope) to a [Relying Party](#relying-party) with the present user’s account (the account might already exist or might be created at this time). The second is [Authentication](#authentication), where the [Relying Party](#relying-party) is presented with an *[Authentication Assertion](#authentication-assertion)* proving the presence and of the user who registered the [public key credential](#public-key-credential). Functionally, the [Web Authentication API](#web-authentication-api) comprises a `PublicKeyCredential` which extends the Credential Management API [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1"), and infrastructure which allows those credentials to be used with `navigator.credentials.create()` and `navigator.credentials.get()`. The former is used during [Registration](#registration), and the latter during [Authentication](#authentication).

Broadly, compliant [authenticators](#authenticator) protect [public key credentials](#public-key-credential), and interact with user agents to implement the [Web Authentication API](#web-authentication-api). Implementing compliant authenticators is possible in software executing (a) on a general-purpose computing device, (b) on an on-device Secure Execution Environment, Trusted Platform Module (TPM), or a Secure Element (SE), or (c) off device. Authenticators being implemented on device are called [platform authenticators](#platform-authenticators). Authenticators being implemented off device ([roaming authenticators](#roaming-authenticators)) can be accessed over a transport such as Universal Serial Bus (USB), Bluetooth Low Energy (BLE), or Near Field Communications (NFC).

### 1.1. Specification Roadmap

While many W3C specifications are directed primarily to user agent developers and also to web application developers (i.e., "Web authors"), the nature of Web Authentication requires that this specification be correctly used by multiple audiences, as described below.

**All audiences** ought to begin with [§ 1.2 Use Cases](#sctn-use-cases), [§ 1.3 Sample API Usage Scenarios](#sctn-sample-scenarios), and [§ 4 Terminology](#sctn-terminology), and should also refer to [\[WebAuthnAPIGuide\]](#biblio-webauthnapiguide "Web Authentication API Guide") for an overall tutorial. Beyond that, the intended audiences for this document are the following main groups:

Note: Along with the [Web Authentication API](#sctn-api) itself, this specification defines a request-response *cryptographic protocol* —the WebAuthn/FIDO2 protocol —between a [WebAuthn Relying Party](#webauthn-relying-party) server and an [authenticator](#authenticator), where the [Relying Party](#relying-party) ’s request consists of a [challenge](#sctn-cryptographic-challenges) and other input data supplied by the [Relying Party](#relying-party) and sent to the [authenticator](#authenticator). The request is conveyed via the combination of HTTPS, the [Relying Party](#relying-party) [web application](#web-application), the [WebAuthn API](#sctn-api), and the platform-specific communications channel between the user agent and the [authenticator](#authenticator). The [authenticator](#authenticator) replies with a digitally signed [authenticator data](#authenticator-data) message and other output data, which is conveyed back to the [Relying Party](#relying-party) server via the same path in reverse. Protocol details vary according to whether an [authentication](#authentication) or [registration](#registration) operation is invoked by the [Relying Party](#relying-party). See also [Figure 1](#fig-registration) and [Figure 2](#fig-authentication).

**It is important for Web Authentication deployments' end-to-end security** that the role of each component—the [Relying Party](#relying-party) server, the [client](#client), and the [authenticator](#authenticator) — as well as [§ 13 Security Considerations](#sctn-security-considerations) and [§ 14 Privacy Considerations](#sctn-privacy-considerations), are understood *by all audiences*.

### 1.2. Use Cases

The below use case scenarios illustrate use of two very different types of [authenticators](#authenticator) and credentials across two common deployment types, as well as outline further scenarios. Additional scenarios, including sample code, are given later in [§ 1.3 Sample API Usage Scenarios](#sctn-sample-scenarios). These examples are for illustrative purposes only, and feature availability may differ between client and authenticator implementations.

#### 1.2.1. Consumer with Multi-Device Credentials

This use case illustrates how a consumer-centric [Relying Party](#relying-party) can leverage the authenticator built-in to a user’s devices to provide phishing-resistant sign in using [multi-device credentials](#multi-device-credential) (commonly referred to as synced [passkeys](#passkey)).

##### 1.2.1.1. Registration

- On a phone:
	- User navigates to example.com in a browser and signs in to an existing account using whatever method they have been using (possibly a legacy method such as a password), or creates a new account.
		- The phone prompts, "Do you want to create a passkey for example.com?"
		- User agrees.
		- The phone prompts the user for a previously configured [authorization gesture](#authorization-gesture) (PIN, biometric, etc.); the user provides this.
		- Website shows message, "Registration complete."

##### 1.2.1.2. Authentication

- On a laptop or desktop:
	- User navigates to example.com in a browser and initiates signing in.
		- If the [multi-device credential](#multi-device-credential) (commonly referred to as a synced [passkey](#passkey)) is available on the device:
		- The browser or operating system prompts the user for a previously configured [authorization gesture](#authorization-gesture) (PIN, biometric, etc.); the user provides this.
				- Web page shows that the selected user is signed in, and navigates to the signed-in page.
		- If the synced [passkey](#passkey) is not available on the device:
		- The browser or operating system prompts the user for an external authenticator, such as a phone or security key.
				- The user selects a previously linked phone.
- Next, on their phone:
	- User sees a discrete prompt or notification, "Sign in to example.com."
		- User selects this prompt / notification.
		- User is shown a list of their example.com identities, e.g., "Sign in as Mohamed / Sign in as 张三".
		- User picks an identity, is prompted for an [authorization gesture](#authorization-gesture) (PIN, biometric, etc.) and provides this.
- Now, back on the laptop:
	- Web page shows that the selected user is signed in, and navigates to the signed-in page.

#### 1.2.2. Workforce with Single-Device Credentials

This use case illustrates how a workforce-centric [Relying Party](#relying-party) can leverage a combination of a [roaming authenticator](#roaming-authenticators) (e.g., a USB security key) and a [platform authenticator](#platform-authenticators) (e.g., a built-in fingerprint sensor) such that the user has:

- a "primary" [roaming authenticator](#roaming-authenticators) that they use to authenticate on new-to-them [client devices](#client-device) (e.g., laptops, desktops) or on such [client devices](#client-device) that lack a [platform authenticator](#platform-authenticators), and
- a low-friction means to strongly re-authenticate on [client devices](#client-device) having [platform authenticators](#platform-authenticators), or
- a means to strongly re-authenticate on [client devices](#client-device) having [passkey platform authenticators](#passkey-platform-authenticator) which do not support [single-device credentials](#single-device-credential) (commonly referred to as device-bound [passkeys](#passkey)).

##### 1.2.2.1. Registration

In this example, the user’s employer mails a security key which is preconfigured with a device-bound [passkey](#passkey).

A temporary PIN was sent to the user out of band (e.g., via an RCS message).

##### 1.2.2.2. Authentication

- On a laptop or desktop:
	- User navigates to corp.example.com in a browser and initiates signing in.
		- The browser or operating system prompts the user for their security key.
		- The user connects their USB security key.
		- The USB security key blinks to indicate the user should press the button on it; the user does.
		- The browser or operating system asks the user to enter their PIN.
		- The user enters the temporary PIN they were provided and continues.
		- The browser or operating system informs the user that they must change their PIN and prompts for a new one.
		- The user enters their new PIN and continues.
		- The browser or operating system restarts the authentication flow and asks the user to enter their PIN.
		- The user enters their new PIN and taps the security key.
		- Web page shows that the selected user is signed in, and navigates to the signed-in page.

#### 1.2.3. Other Use Cases and Configurations

A variety of additional use cases and configurations are also possible, including (but not limited to):

- A user navigates to example.com on their laptop, is guided through a flow to create and register a credential on their phone.
- A user obtains a discrete, [roaming authenticator](#roaming-authenticators), such as a security key with USB and/or NFC connectivity options, loads example.com in their browser on a laptop or phone, and is guided through a flow to create and register a credential on the security key.
- A [Relying Party](#relying-party) prompts the user for their [authorization gesture](#authorization-gesture) in order to authorize a single transaction, such as a payment or other financial transaction.

### 1.3. Sample API Usage Scenarios

*This section is not normative.*

In this section, we walk through some events in the lifecycle of a [public key credential](#public-key-credential), along with the corresponding sample code for using this API. Note that this is an example flow and does not limit the scope of how the API can be used.

As was the case in earlier sections, this flow focuses on a use case involving a [passkey roaming authenticator](#passkey-roaming-authenticator) with its own display. One example of such an authenticator would be a smart phone. Other authenticator types are also supported by this API, subject to implementation by the [client platform](#client-platform). For instance, this flow also works without modification for the case of an authenticator that is embedded in the [client device](#client-device). The flow also works for the case of an authenticator without its own display (similar to a smart card) subject to specific implementation considerations. Specifically, the [client platform](#client-platform) needs to display any prompts that would otherwise be shown by the authenticator, and the authenticator needs to allow the [client platform](#client-platform) to enumerate all the authenticator’s credentials so that the client can have information to show appropriate prompts.

#### 1.3.1. Registration

This is the first-time flow, in which a new credential is created and registered with the server. In this flow, the [WebAuthn Relying Party](#webauthn-relying-party) does not have a preference for [platform authenticator](#platform-authenticators) or [roaming authenticators](#roaming-authenticators).

1. The user visits example.com, which serves up a script. At this point, the user may already be logged in using a legacy username and password, or additional authenticator, or other means acceptable to the [Relying Party](#relying-party). Or the user may be in the process of creating a new account.
2. The [Relying Party](#relying-party) script runs the code snippet below.
3. The [client platform](#client-platform) searches for and locates the authenticator.
4. The [client](#client) connects to the authenticator, performing any pairing actions if necessary.
5. The authenticator shows appropriate UI for the user to provide a biometric or other [authorization gesture](#authorization-gesture).
6. The authenticator returns a response to the [client](#client), which in turn returns a response to the [Relying Party](#relying-party) script. If the user declined to select an authenticator or provide authorization, an appropriate error is returned.
7. If a new credential was created,
	- The [Relying Party](#relying-party) script sends the newly generated [credential public key](#credential-public-key) to the server, along with additional information such as attestation regarding the provenance and characteristics of the authenticator.
		- The server stores the [credential public key](#credential-public-key) in its database and associates it with the user as well as with the characteristics of authentication indicated by attestation, also storing a friendly name for later use.
		- The script may store data such as the [credential ID](#credential-id) in local storage, to improve future UX by narrowing the choice of credential for the user.

The sample code for generating and registering a new key follows:

```
if (!window.PublicKeyCredential) { /* Client not capable. Handle error. */ }

var publicKey = {
  // The challenge is produced by the server; see the Security Considerations
  challenge: new Uint8Array([21,31,105 /* 29 more random bytes generated by the server */]),

  // Relying Party:
  rp: {
    name: "ACME Corporation"
  },

  // User:
  user: {
    id: Uint8Array.from(window.atob("MIIBkzCCATigAwIBAjCCAZMwggE4oAMCAQIwggGTMII="), c=>c.charCodeAt(0)),
    name: "alex.mueller@example.com",
    displayName: "Alex Müller",
  },

  // This Relying Party will accept either an EdDSA, ES256 or RS256 credential, but
  // prefers an EdDSA credential.
  pubKeyCredParams: [
    {
      type: "public-key",
      alg: -8 // "EdDSA" as registered in the IANA COSE Algorithms registry
    },
    {
      type: "public-key",
      alg: -7 // "ES256" as registered in the IANA COSE Algorithms registry
    },
    {
      type: "public-key",
      alg: -257 // Value registered by this specification for "RS256"
    }
  ],

  authenticatorSelection: {
    // Try to use UV if possible. This is also the default.
    userVerification: "preferred"
  },

  timeout: 300000,  // 5 minutes
  excludeCredentials: [
    // Don't re-register any authenticator that has one of these credentials
    {"id": Uint8Array.from(window.atob("ufJWp8YGlibm1Kd9XQBWN1WAw2jy5In2Xhon9HAqcXE="), c=>c.charCodeAt(0)), "type": "public-key"},
    {"id": Uint8Array.from(window.atob("E/e1dhZc++mIsz4f9hb6NifAzJpF1V4mEtRlIPBiWdY="), c=>c.charCodeAt(0)), "type": "public-key"}
  ],

  // Make excludeCredentials check backwards compatible with credentials registered with U2F
  extensions: {"appidExclude": "https://acme.example.com"}
};

// Note: The following call will cause the authenticator to display UI.
navigator.credentials.create({ publicKey })
  .then(function (newCredentialInfo) {
    // Send new credential info to server for verification and registration.
  }).catch(function (err) {
    // No acceptable authenticator or user refused consent. Handle appropriately.
  });
```

#### 1.3.2. Registration Specifically with User-Verifying Platform Authenticator

This is an example flow for when the [WebAuthn Relying Party](#webauthn-relying-party) is specifically interested in creating a [public key credential](#public-key-credential) with a [user-verifying platform authenticator](#user-verifying-platform-authenticator).

1. The user visits example.com and clicks on the login button, which redirects the user to login.example.com.
2. The user enters a username and password to log in. After successful login, the user is redirected back to example.com.
3. The [Relying Party](#relying-party) script runs the code snippet below.
	1. The user agent checks if a [user-verifying platform authenticator](#user-verifying-platform-authenticator) is available. If not, terminate this flow.
		2. The [Relying Party](#relying-party) asks the user if they want to create a credential with it. If not, terminate this flow.
		3. The user agent and/or operating system shows appropriate UI and guides the user in creating a credential using one of the available platform authenticators.
		4. Upon successful credential creation, the [Relying Party](#relying-party) script conveys the new credential to the server.
```
if (!window.PublicKeyCredential) { /* Client not capable of the API. Handle error. */ }

PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable()
    .then(function (uvpaAvailable) {
        // If there is a user-verifying platform authenticator
        if (uvpaAvailable) {
            // Render some RP-specific UI and get a Promise for a Boolean value
            return askIfUserWantsToCreateCredential();
        }
    }).then(function (userSaidYes) {
        // If there is a user-verifying platform authenticator
        // AND the user wants to create a credential
        if (userSaidYes) {
            var publicKeyOptions = { /* Public key credential creation options. */};
            return navigator.credentials.create({ "publicKey": publicKeyOptions });
        }
    }).then(function (newCredentialInfo) {
        if (newCredentialInfo) {
            // Send new credential info to server for verification and registration.
        }
    }).catch(function (err) {
        // Something went wrong. Handle appropriately.
    });
```

#### 1.3.3. Authentication

This is the flow when a user with an already registered credential visits a website and wants to authenticate using the credential.

1. The user visits example.com, which serves up a script.
2. The script asks the [client](#client) for an Authentication Assertion, providing as much information as possible to narrow the choice of acceptable credentials for the user. This can be obtained from the data that was stored locally after registration, or by other means such as prompting the user for a username.
3. The [Relying Party](#relying-party) script runs one of the code snippets below.
4. The [client platform](#client-platform) searches for and locates the authenticator.
5. The [client](#client) connects to the authenticator, performing any pairing actions if necessary.
6. The authenticator presents the user with a notification that their attention is needed. On opening the notification, the user is shown a friendly selection menu of acceptable credentials using the account information provided when creating the credentials, along with some information on the [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) that is requesting these keys.
7. The authenticator obtains a biometric or other [authorization gesture](#authorization-gesture) from the user.
8. The authenticator returns a response to the [client](#client), which in turn returns a response to the [Relying Party](#relying-party) script. If the user declined to select a credential or provide an authorization, an appropriate error is returned.
9. If an assertion was successfully generated and returned,
	- The script sends the assertion to the server.
		- The server examines the assertion, extracts the [credential ID](#credential-id), looks up the registered credential public key in its database, and verifies the [assertion signature](#assertion-signature). If valid, it looks up the identity associated with the assertion’s [credential ID](#credential-id); that identity is now authenticated. If the [credential ID](#credential-id) is not recognized by the server (e.g., it has been deregistered due to inactivity) then the authentication has failed; each [Relying Party](#relying-party) will handle this in its own way.
		- The server now does whatever it would otherwise do upon successful authentication -- return a success page, set authentication cookies, etc.

If the [Relying Party](#relying-party) script does not have any hints available (e.g., from locally stored data) to help it narrow the list of credentials, then the sample code for performing such an authentication might look like this:

```
if (!window.PublicKeyCredential) { /* Client not capable. Handle error. */ }

// credentialId is generated by the authenticator and is an opaque random byte array
var credentialId = new Uint8Array([183, 148, 245 /* more random bytes previously generated by the authenticator */]);
var options = {
  // The challenge is produced by the server; see the Security Considerations
  challenge: new Uint8Array([4,101,15 /* 29 more random bytes generated by the server */]),
  timeout: 300000,  // 5 minutes
  allowCredentials: [{ type: "public-key", id: credentialId }]
};

navigator.credentials.get({ "publicKey": options })
    .then(function (assertion) {
    // Send assertion to server for verification
}).catch(function (err) {
    // No acceptable credential or user refused consent. Handle appropriately.
});
```

On the other hand, if the [Relying Party](#relying-party) script has some hints to help it narrow the list of credentials, then the sample code for performing such an authentication might look like the following. Note that this sample also demonstrates how to use the [Credential Properties Extension](#credprops).

```
if (!window.PublicKeyCredential) { /* Client not capable. Handle error. */ }

var encoder = new TextEncoder();
var acceptableCredential1 = {
    type: "public-key",
    id: encoder.encode("BA44712732CE")
};
var acceptableCredential2 = {
    type: "public-key",
    id: encoder.encode("BG35122345NF")
};

var options = {
  // The challenge is produced by the server; see the Security Considerations
  challenge: new Uint8Array([8,18,33 /* 29 more random bytes generated by the server */]),
  timeout: 300000,  // 5 minutes
  allowCredentials: [acceptableCredential1, acceptableCredential2],
  extensions: { 'credProps': true }
};

navigator.credentials.get({ "publicKey": options })
    .then(function (assertion) {
    // Send assertion to server for verification
}).catch(function (err) {
    // No acceptable credential or user refused consent. Handle appropriately.
});
```

#### 1.3.4. Aborting Authentication Operations

The below example shows how a developer may use the AbortSignal parameter to abort a credential registration operation. A similar procedure applies to an authentication operation.

```
const authAbortController = new AbortController();
const authAbortSignal = authAbortController.signal;

authAbortSignal.onabort = function () {
    // Once the page knows the abort started, inform user it is attempting to abort.
}

var options = {
    // A list of options.
}

navigator.credentials.create({
    publicKey: options,
    signal: authAbortSignal})
    .then(function (attestation) {
        // Register the user.
    }).catch(function (error) {
        if (error.name === "AbortError") {
            // Inform user the credential hasn't been created.
            // Let the server know a key hasn't been created.
        }
    });

// Assume widget shows up whenever authentication occurs.
if (widget == "disappear") {
    authAbortController.abort();
}
```

#### 1.3.5. Decommissioning

The following are possible situations in which decommissioning a credential might be desired. Note that all of these are handled on the server side and do not need support from the API specified here.

- Possibility #1 -- user reports the credential as lost.
	- User goes to server.example.net, authenticates and follows a link to report a lost/stolen [authenticator](#authenticator).
		- Server returns a page showing the list of registered credentials with friendly names as configured during registration.
		- User selects a credential and the server deletes it from its database.
		- In the future, the [Relying Party](#relying-party) script does not specify this credential in any list of acceptable credentials, and assertions signed by this credential are rejected.
- Possibility #2 -- server deregisters the credential due to inactivity.
	- Server deletes credential from its database during maintenance activity.
		- In the future, the [Relying Party](#relying-party) script does not specify this credential in any list of acceptable credentials, and assertions signed by this credential are rejected.
- Possibility #3 -- user deletes the credential from the [authenticator](#authenticator).
	- User employs a [authenticator](#authenticator) -specific method (e.g., device settings UI) to delete a credential from their [authenticator](#authenticator).
		- From this point on, this credential will not appear in any selection prompts, and no assertions can be generated with it.
		- Sometime later, the server deregisters this credential due to inactivity.

### 1.4. Platform-Specific Implementation Guidance

This specification defines how to use Web Authentication in the general case. When using Web Authentication in connection with specific platform support (e.g. apps), it is recommended to see platform-specific documentation and guides for additional guidance and limitations.

## 2\. Conformance

This specification defines three conformance classes. Each of these classes is specified so that conforming members of the class are secure against non-conforming or hostile members of the other classes.

### 2.1. User Agents

A User Agent MUST behave as described by [§ 5 Web Authentication API](#sctn-api) in order to be considered conformant. [Conforming User Agents](#conforming-user-agent) MAY implement algorithms given in this specification in any way desired, so long as the end result is indistinguishable from the result that would be obtained by the specification’s algorithms.

A conforming User Agent MUST also be a conforming implementation of the IDL fragments of this specification, as described in the “Web IDL” specification. [\[WebIDL\]](#biblio-webidl "Web IDL Standard")

#### 2.1.1. Enumerations as DOMString types

Enumeration types are not referenced by other parts of the Web IDL because that would preclude other values from being used without updating this specification and its implementations. It is important for backwards compatibility that [client platforms](#client-platform) and [Relying Parties](#relying-party) handle unknown values. Enumerations for this specification exist here for documentation and as a registry. Where the enumerations are represented elsewhere, they are typed as `DOMString` s, for example in `transports`.

### 2.2. Authenticators

A [WebAuthn Authenticator](#webauthn-authenticator) MUST provide the operations defined by [§ 6 WebAuthn Authenticator Model](#sctn-authenticator-model), and those operations MUST behave as described there. This is a set of functional and security requirements for an authenticator to be usable by a [Conforming User Agent](#conforming-user-agent).

As described in [§ 1.2 Use Cases](#sctn-use-cases), an authenticator may be implemented in the operating system underlying the User Agent, or in external hardware, or a combination of both.

#### 2.2.1. Backwards Compatibility with FIDO U2F

[Authenticators](#authenticator) that only support the [§ 8.6 FIDO U2F Attestation Statement Format](#sctn-fido-u2f-attestation) have no mechanism to store a [user handle](#user-handle), so the returned `userHandle` will always be null.

### 2.3. WebAuthn Relying Parties

A [WebAuthn Relying Party](#webauthn-relying-party) MUST behave as described in [§ 7 WebAuthn Relying Party Operations](#sctn-rp-operations) to obtain all the security benefits offered by this specification. See [§ 13.4.1 Security Benefits for WebAuthn Relying Parties](#sctn-rp-benefits) for further discussion of this.

### 2.4. All Conformance Classes

All [CBOR](#cbor) encoding performed by the members of the above conformance classes MUST be done using the. All decoders of the above conformance classes SHOULD reject CBOR that is not validly encoded in the and SHOULD reject messages with duplicate map keys.

## 3\. Dependencies

This specification relies on several other underlying specifications, listed below and in [Terms defined by reference](#index-defined-elsewhere).

Base64url encoding

The term Base64url Encoding refers to the base64 encoding using the URL- and filename-safe character set defined in Section 5 of [\[RFC4648\]](#biblio-rfc4648 "The Base16, Base32, and Base64 Data Encodings"), with all trailing '=' characters omitted (as permitted by Section 3.2) and without the inclusion of any line breaks, whitespace, or other additional characters.

CBOR

A number of structures in this specification, including attestation statements and extensions, are encoded using the of the Compact Binary Object Representation (CBOR) [\[RFC8949\]](#biblio-rfc8949 "Concise Binary Object Representation (CBOR)"), as defined in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)").

CDDL

This specification describes the syntax of all [CBOR](#cbor) -encoded data using the CBOR Data Definition Language (CDDL) [\[RFC8610\]](#biblio-rfc8610 "Concise Data Definition Language (CDDL): A Notational Convention to Express Concise Binary Object Representation (CBOR) and JSON Data Structures").

COSE

CBOR Object Signing and Encryption (COSE) [\[RFC9052\]](#biblio-rfc9052 "CBOR Object Signing and Encryption (COSE): Structures and Process") [\[RFC9053\]](#biblio-rfc9053 "CBOR Object Signing and Encryption (COSE): Initial Algorithms"). The IANA COSE Algorithms registry [\[IANA-COSE-ALGS-REG\]](#biblio-iana-cose-algs-reg "IANA CBOR Object Signing and Encryption (COSE) Algorithms Registry") originally established by [\[RFC8152\]](#biblio-rfc8152 "CBOR Object Signing and Encryption (COSE)") and updated by these specifications is also used.

Credential Management

The API described in this document is an extension of the `Credential` concept defined in [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1").

DOM

`DOMException` and the DOMException values used in this specification are defined in [\[DOM4\]](#biblio-dom4 "DOM Standard").

ECMAScript

[%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor) is defined in [\[ECMAScript\]](#biblio-ecmascript "ECMAScript Language Specification").

URL

The concepts of [domain](https://url.spec.whatwg.org/#concept-domain), [host](https://url.spec.whatwg.org/#concept-url-host), [port](https://url.spec.whatwg.org/#concept-url-port), [scheme](https://url.spec.whatwg.org/#concept-url-scheme), [valid domain](https://url.spec.whatwg.org/#valid-domain) and [valid domain string](https://url.spec.whatwg.org/#valid-domain-string) are defined in [\[URL\]](#biblio-url "URL Standard").

Web IDL

Many of the interface definitions and all of the IDL in this specification depend on [\[WebIDL\]](#biblio-webidl "Web IDL Standard"). This updated version of the Web IDL standard adds support for `Promise` s, which are now the preferred mechanism for asynchronous interaction in all new web APIs.

FIDO AppID

The algorithms for [determining the FacetID of a calling application](https://fidoalliance.org/specs/fido-v2.0-id-20180227/fido-appid-and-facets-v2.0-id-20180227.html#determining-the-facetid-of-a-calling-application) and [determining if a caller’s FacetID is authorized for an AppID](https://fidoalliance.org/specs/fido-v2.0-id-20180227/fido-appid-and-facets-v2.0-id-20180227.html#determining-if-a-caller-s-facetid-is-authorized-for-an-appid) (used only in the [AppID extension](#appid)) are defined by [\[FIDO-APPID\]](#biblio-fido-appid "FIDO AppID and Facet Specification").

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "NOT RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in BCP 14 [\[RFC2119\]](#biblio-rfc2119 "Key words for use in RFCs to Indicate Requirement Levels") [\[RFC8174\]](#biblio-rfc8174 "Ambiguity of Uppercase vs Lowercase in RFC 2119 Key Words") when, and only when, they appear in all capitals, as shown here.

## 4\. Terminology

Attestation

Generally, *attestation* is a statement that serves to bear witness, confirm, or authenticate. In the WebAuthn context, [attestation](#attestation) is employed to provide verifiable evidence as to the origin of an [authenticator](#authenticator) and the data it emits. This includes such things as [credential IDs](#credential-id), [credential key pairs](#credential-key-pair), [signature counters](#signature-counter), etc.

An [attestation statement](#attestation-statement) is provided within an [attestation object](#attestation-object) during a [registration](#registration) ceremony. See also [§ 6.5 Attestation](#sctn-attestation) and [Figure 6](#fig-attStructs). Whether or how the [client](#client) conveys the [attestation statement](#attestation-statement) and [aaguid](#authdata-attestedcredentialdata-aaguid) portions of the [attestation object](#attestation-object) to the [Relying Party](#relying-party) is described by [attestation conveyance](#attestation-conveyance).

Attestation Certificate

An X.509 Certificate for the attestation key pair used by an [authenticator](#authenticator) to attest to its manufacture and capabilities. At [registration](#registration) time, the [authenticator](#authenticator) uses the attestation private key to sign the [Relying Party](#relying-party) -specific [credential public key](#credential-public-key) (and additional data) that it generates and returns via the [authenticatorMakeCredential](#authenticatormakecredential) operation. [Relying Parties](#relying-party) use the attestation public key conveyed in the [attestation certificate](#attestation-certificate) to verify the [attestation signature](#attestation-signature). Note that in the case of [self attestation](#self-attestation), the [authenticator](#authenticator) has no distinct [attestation key pair](#attestation-key-pair) nor [attestation certificate](#attestation-certificate), see [self attestation](#self-attestation) for details.

Authentication

Authentication Ceremony

The [ceremony](#ceremony) where a user, and the user’s [client platform](#client-platform) (containing or connected to at least one [authenticator](#authenticator)) work in concert to cryptographically prove to a [Relying Party](#relying-party) that the user controls the [credential private key](#credential-private-key) of a previously-registered [public key credential](#public-key-credential) (see [Registration](#registration)). Note that this includes a [test of user presence](#test-of-user-presence) or [user verification](#user-verification).

The WebAuthn [authentication ceremony](#authentication-ceremony) is defined in [§ 7.2 Verifying an Authentication Assertion](#sctn-verifying-assertion), and is initiated by the [Relying Party](#relying-party) invoking a `` `navigator.credentials.get()` `` operation with a `publicKey` argument. See [§ 5 Web Authentication API](#sctn-api) for an introductory overview and [§ 1.3.3 Authentication](#sctn-sample-authentication) for implementation examples.

Authentication Assertion

Assertion

The cryptographically signed `AuthenticatorAssertionResponse` object returned by an [authenticator](#authenticator) as the result of an [authenticatorGetAssertion](#authenticatorgetassertion) operation.

This corresponds to the [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1") specification’s single-use [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential).

Authenticator

WebAuthn Authenticator

A cryptographic entity, existing in hardware or software, that can [register](#registration) a user with a given [Relying Party](#relying-party) and later [assert possession](#authentication-assertion) of the registered [public key credential](#public-key-credential), and optionally [verify the user](#user-verification) to the [Relying Party](#relying-party). [Authenticators](#authenticator) can report information regarding their [type](#authenticator-type) and security characteristics via [attestation](#attestation) during [registration](#registration) and [assertion](#assertion).

A [WebAuthn Authenticator](#webauthn-authenticator) could be a [roaming authenticator](#roaming-authenticators), a dedicated hardware subsystem integrated into the [client device](#client-device), or a software component of the [client](#client) or [client device](#client-device). A [WebAuthn Authenticator](#webauthn-authenticator) is not necessarily confined to operating in a local context, and can generate or store a [credential key pair](#credential-key-pair) in a server outside of [client-side](#client-side) hardware.

In general, an [authenticator](#authenticator) is assumed to have only one user. If multiple natural persons share access to an [authenticator](#authenticator), they are considered to represent the same user in the context of that [authenticator](#authenticator). If an [authenticator](#authenticator) implementation supports multiple users in separated compartments, then each compartment is considered a separate [authenticator](#authenticator) with a single user with no access to other users' [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential).

Authorization Gesture

An [authorization gesture](#authorization-gesture) is a physical interaction performed by a user with an authenticator as part of a [ceremony](#ceremony), such as [registration](#registration) or [authentication](#authentication). By making such an [authorization gesture](#authorization-gesture), a user for (i.e., *authorizes*) a [ceremony](#ceremony) to proceed. This MAY involve [user verification](#user-verification) if the employed [authenticator](#authenticator) is capable, or it MAY involve a simple [test of user presence](#test-of-user-presence).

Backed Up

[Public Key Credential Sources](#public-key-credential-source) may be backed up in some fashion such that they may become present on an authenticator other than their [generating authenticator](#generating-authenticator). Backup can occur via mechanisms including but not limited to peer-to-peer sync, cloud sync, local network sync, and manual import/export. See also [§ 6.1.3 Credential Backup State](#sctn-credential-backup).

Backup Eligibility

Backup Eligible

A [Public Key Credential Source](#public-key-credential-source) ’s [generating authenticator](#generating-authenticator) determines at creation time whether the [public key credential source](#public-key-credential-source) is allowed to be [backed up](#backed-up). Backup eligibility is signaled in [authenticator data](#authenticator-data) ’s [flags](#authdata-flags) along with the current [backup state](#backup-state). Backup eligibility is a [credential property](#credential-properties) and is permanent for a given [public key credential source](#public-key-credential-source). A backup eligible [public key credential source](#public-key-credential-source) is referred to as a multi-device credential whereas one that is not backup eligible is referred to as a single-device credential. See also [§ 6.1.3 Credential Backup State](#sctn-credential-backup).

Backup State

The current backup state of a [multi-device credential](#multi-device-credential) as determined by the current [managing authenticator](#public-key-credential-source-managing-authenticator). Backup state is signaled in [authenticator data](#authenticator-data) ’s [flags](#authdata-flags) and can change over time. See also [backup eligibility](#backup-eligibility) and [§ 6.1.3 Credential Backup State](#sctn-credential-backup).

Biometric Authenticator

Any [authenticator](#authenticator) that implements [biometric recognition](#biometric-recognition).

Biometric Recognition

The automated recognition of individuals based on their biological and behavioral characteristics [\[ISOBiometricVocabulary\]](#biblio-isobiometricvocabulary "Information technology — Vocabulary — Biometrics").

Bound credential

"Authenticator [contains](#contains) a credential"

"Credential [created on](#created-on) an authenticator"

A [public key credential source](#public-key-credential-source) or [public key credential](#public-key-credential) is said to be [bound](#bound-credential) to its [managing authenticator](#public-key-credential-source-managing-authenticator). This means that only the [managing authenticator](#public-key-credential-source-managing-authenticator) can generate [assertions](#assertion) for the [public key credential sources](#public-key-credential-source) [bound](#bound-credential) to it.

This may also be expressed as "the [managing authenticator](#public-key-credential-source-managing-authenticator) contains the [bound credential](#bound-credential) ", or "the [bound credential](#bound-credential) was created on its [managing authenticator](#public-key-credential-source-managing-authenticator) ". Note, however, that a [server-side credential](#server-side-credential) might not be physically stored in persistent memory inside the authenticator, hence " [bound to](#bound-credential) " is the primary term. See [§ 6.2.2 Credential Storage Modality](#sctn-credential-storage-modality).

Ceremony

The concept of a [ceremony](#ceremony) [\[Ceremony\]](#biblio-ceremony "Ceremony Design and Analysis") is an extension of the concept of a network protocol, with human nodes alongside computer nodes and with communication links that include user interface(s), human-to-human communication, and transfers of physical objects that carry data. What is out-of-band to a protocol is in-band to a ceremony. In this specification, [Registration](#registration) and [Authentication](#authentication) are ceremonies, and an [authorization gesture](#authorization-gesture) is often a component of those [ceremonies](#ceremony).

Client

WebAuthn Client

Also referred to herein as simply a [client](#client). See also [Conforming User Agent](#conforming-user-agent). A [WebAuthn Client](#webauthn-client) is an intermediary entity typically implemented in the user agent (in whole, or in part). Conceptually, it underlies the [Web Authentication API](#web-authentication-api) and embodies the implementation of the `[[Create]](origin, options, sameOriginWithAncestors)` and `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` [internal methods](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots). It is responsible for both marshalling the inputs for the underlying [authenticator operations](#authenticator-operations), and for returning the results of the latter operations to the [Web Authentication API](#web-authentication-api) ’s callers.

The [WebAuthn Client](#webauthn-client) runs on, and is distinct from, a [WebAuthn Client Device](#webauthn-client-device).

Client Device

WebAuthn Client Device

The hardware device on which the [WebAuthn Client](#webauthn-client) runs, for example a smartphone, a laptop computer or a desktop computer, and the operating system running on that hardware.

The distinctions between a [WebAuthn Client device](#webauthn-client-device) and a [client](#client) are:

- a single [client device](#client-device) MAY support running multiple [clients](#client), i.e., browser implementations, which all have access to the same [authenticators](#authenticator) available on that [client device](#client-device), and
- [platform authenticators](#platform-authenticators) are bound to a [client device](#client-device) rather than a [WebAuthn Client](#webauthn-client).

A [client device](#client-device) and a [client](#client) together constitute a [client platform](#client-platform).

Client Platform

A [client device](#client-device) and a [client](#client) together make up a [client platform](#client-platform). A single hardware device MAY be part of multiple distinct [client platforms](#client-platform) at different times by running different operating systems and/or [clients](#client).

Client-Side

This refers in general to the combination of the user’s [client platform](#client-platform), [authenticators](#authenticator), and everything gluing it all together.

Client-side discoverable Public Key Credential Source

Client-side discoverable Credential

Discoverable Credential

Passkey

\[DEPRECATED\] Resident Credential

\[DEPRECATED\] Resident Key

Note: Historically, [client-side discoverable credentials](#client-side-discoverable-credential) have been known as [resident credentials](#resident-credential) or [resident keys](#resident-key). Due to the phrases `ResidentKey` and `residentKey` being widely used in both the [WebAuthn API](#web-authentication-api) and also in the [Authenticator Model](#authenticator-model) (e.g., in dictionary member names, algorithm variable names, and operation parameters) the usage of `resident` within their names has not been changed for backwards compatibility purposes. Also, the term [resident key](#resident-key) is defined here as equivalent to a [client-side discoverable credential](#client-side-discoverable-credential).

A [Client-side discoverable Public Key Credential Source](#client-side-discoverable-public-key-credential-source), or [Discoverable Credential](#discoverable-credential) for short, is a [public key credential source](#public-key-credential-source) that is ***discoverable*** and usable in [authentication ceremonies](#authentication-ceremony) where the [Relying Party](#relying-party) does not provide any [credential ID](#credential-id) s, i.e., the [Relying Party](#relying-party) invokes `navigator.credentials.get()` with an ***[empty](https://infra.spec.whatwg.org/#list-is-empty)*** `allowCredentials` argument. This means that the [Relying Party](#relying-party) does not necessarily need to first identify the user.

As a consequence, a [discoverable credential capable](#discoverable-credential-capable) [authenticator](#authenticator) can generate an [assertion signature](#assertion-signature) for a [discoverable credential](#discoverable-credential) given only an [RP ID](#rp-id), which in turn necessitates that the [public key credential source](#public-key-credential-source) is stored in the [authenticator](#authenticator) or [client platform](#client-platform). This is in contrast to a [Server-side Public Key Credential Source](#server-side-public-key-credential-source), which requires that the [authenticator](#authenticator) is given both the [RP ID](#rp-id) and the [credential ID](#credential-id) but does not require [client-side](#client-side) storage of the [public key credential source](#public-key-credential-source).

See also: and [non-discoverable credential](#non-discoverable-credential).

Note: [Client-side discoverable credentials](#client-side-discoverable-credential) are also usable in [authentication ceremonies](#authentication-ceremony) where [credential ID](#credential-id) s are given, i.e., when calling `navigator.credentials.get()` with a non- [empty](https://infra.spec.whatwg.org/#list-is-empty) `allowCredentials` argument.

Conforming User Agent

A user agent implementing, in cooperation with the underlying [client device](#client-device), the [Web Authentication API](#web-authentication-api) and algorithms given in this specification, and handling communication between [authenticators](#authenticator) and [Relying Parties](#relying-party).

Credential ID

A probabilistically-unique [byte sequence](https://infra.spec.whatwg.org/#byte-sequence) identifying a [public key credential source](#public-key-credential-source) and its [authentication assertions](#authentication-assertion). At most 1023 bytes long.

Credential IDs are generated by [authenticators](#authenticator) in two forms:

1. At least 16 bytes that include at least 100 bits of entropy, or
2. The [public key credential source](#public-key-credential-source), without its [Credential ID](#credential-id) or [mutable items](#public-key-credential-source-mutable-item), encrypted so only its [managing authenticator](#public-key-credential-source-managing-authenticator) can decrypt it. This form allows the [authenticator](#authenticator) to be nearly stateless, by having the [Relying Party](#relying-party) store any necessary state.
	Note: [\[FIDO-UAF-AUTHNR-CMDS\]](#biblio-fido-uaf-authnr-cmds "FIDO UAF Authenticator Commands") includes guidance on encryption techniques under "Security Guidelines".

[Relying Parties](#relying-party) do not need to distinguish these two [Credential ID](#credential-id) forms.

Credential Key Pair

Credential Private Key

Credential Public Key

User Public Key

A [credential key pair](#credential-key-pair) is a pair of asymmetric cryptographic keys generated by an [authenticator](#authenticator) and [scoped](#scope) to a specific [WebAuthn Relying Party](#webauthn-relying-party). It is the central part of a [public key credential](#public-key-credential).

A [credential public key](#credential-public-key) is the public key portion of a [credential key pair](#credential-key-pair). The [credential public key](#credential-public-key) is returned to the [Relying Party](#relying-party) during a [registration ceremony](#registration-ceremony).

A [credential private key](#credential-private-key) is the private key portion of a [credential key pair](#credential-key-pair). The [credential private key](#credential-private-key) is bound to a particular [authenticator](#authenticator) - its [managing authenticator](#public-key-credential-source-managing-authenticator) - and is expected to never be exposed to any other party, not even to the owner of the [authenticator](#authenticator).

Note that in the case of [self attestation](#self-attestation), the [credential key pair](#credential-key-pair) is also used as the [attestation key pair](#attestation-key-pair), see [self attestation](#self-attestation) for details.

Note: The [credential public key](#credential-public-key) is referred to as the [user public key](#user-public-key) in FIDO UAF [\[UAFProtocol\]](#biblio-uafprotocol "FIDO UAF Protocol Specification v1.0"), and in FIDO U2F [\[FIDO-U2F-Message-Formats\]](#biblio-fido-u2f-message-formats "FIDO U2F Raw Message Formats") and some parts of this specification that relate to it.

Credential Properties

A [credential property](#credential-properties) is some characteristic property of a [public key credential source](#public-key-credential-source), such as whether it is a [client-side discoverable credential](#client-side-discoverable-credential) or a [server-side credential](#server-side-credential).

Credential Record

In order to implement the algorithms defined in [§ 7 WebAuthn Relying Party Operations](#sctn-rp-operations), the [Relying Party](#relying-party) MUST store some properties of registered [public key credential sources](#public-key-credential-source). The [credential record](#credential-record) [struct](https://infra.spec.whatwg.org/#struct) is an abstraction of these properties stored in a [user account](#user-account). A credential record is created during a [registration ceremony](#registration-ceremony) and used in subsequent [authentication ceremonies](#authentication-ceremony). [Relying Parties](#relying-party) MAY delete credential records as necessary or when requested by users.

The following [items](https://infra.spec.whatwg.org/#struct-item) are RECOMMENDED in order to implement all steps of [§ 7.1 Registering a New Credential](#sctn-registering-a-new-credential) and [§ 7.2 Verifying an Authentication Assertion](#sctn-verifying-assertion) as defined:

type

The [type](#public-key-credential-source-type) of the [public key credential source](#public-key-credential-source).

id

The [Credential ID](#credential-id) of the [public key credential source](#public-key-credential-source).

publicKey

The [credential public key](#credential-public-key) of the [public key credential source](#public-key-credential-source).

signCount

The latest value of the [signature counter](#authdata-signcount) in the [authenticator data](#authenticator-data) from any [ceremony](#ceremony) using the [public key credential source](#public-key-credential-source).

transports

The value returned from `getTransports()` when the [public key credential source](#public-key-credential-source) was [registered](#registration).

Note: Modifying or removing [items](https://infra.spec.whatwg.org/#list-item) from the value returned from `getTransports()` could negatively impact user experience, or even prevent use of the corresponding credential.

uvInitialized

A Boolean value indicating whether any [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) from this [public key credential source](#public-key-credential-source) has had the [UV](#authdata-flags-uv) [flag](#authdata-flags) set.

When this is `true`, the [Relying Party](#relying-party) MAY consider the [UV](#authdata-flags-uv) [flag](#authdata-flags) as an [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) in [authentication ceremonies](#authentication-ceremony). For example, a [Relying Party](#relying-party) might skip a password prompt if [uvInitialized](#abstract-opdef-credential-record-uvinitialized) is `true` and the [UV](#authdata-flags-uv) [flag](#authdata-flags) is set, even when [user verification](#user-verification) was not required.

When this is `false`, including an [authentication ceremony](#authentication-ceremony) where it would be updated to `true`, the [UV](#authdata-flags-uv) [flag](#authdata-flags) MUST NOT be relied upon as an [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af). This is because the first time a [public key credential source](#public-key-credential-source) sets the [UV](#authdata-flags-uv) [flag](#authdata-flags) to 1, there is not yet any trust relationship established between the [Relying Party](#relying-party) and the [authenticator](#authenticator) ’s [user verification](#user-verification). Therefore, updating [uvInitialized](#abstract-opdef-credential-record-uvinitialized) from `false` to `true` SHOULD require authorization by an additional [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) equivalent to WebAuthn [user verification](#user-verification).

backupEligible

The value of the [BE](#authdata-flags-be) [flag](#authdata-flags) when the [public key credential source](#public-key-credential-source) was created.

backupState

The latest value of the [BS](#authdata-flags-bs) [flag](#authdata-flags) in the [authenticator data](#authenticator-data) from any [ceremony](#ceremony) using the [public key credential source](#public-key-credential-source).

The following [items](https://infra.spec.whatwg.org/#struct-item) are OPTIONAL:

attestationObject

The value of the `attestationObject` attribute when the [public key credential source](#public-key-credential-source) was [registered](#registration). Storing this enables the [Relying Party](#relying-party) to reference the credential’s [attestation statement](#attestation-statement) at a later time.

attestationClientDataJSON

The value of the `clientDataJSON` attribute when the [public key credential source](#public-key-credential-source) was [registered](#registration). Storing this in combination with the above [attestationObject](#abstract-opdef-credential-record-attestationobject) [item](https://infra.spec.whatwg.org/#struct-item) enables the [Relying Party](#relying-party) to re-verify the [attestation signature](#attestation-signature) at a later time.

[WebAuthn extensions](#webauthn-extensions) MAY define additional [items](https://infra.spec.whatwg.org/#struct-item) needed to process the extension. [Relying Parties](#relying-party) MAY also include any additional [items](https://infra.spec.whatwg.org/#struct-item) as needed, and MAY omit any [items](https://infra.spec.whatwg.org/#struct-item) not needed for their implementation.

The credential descriptor for a credential record is a `PublicKeyCredentialDescriptor` value with the contents:

`type`

The [type](#abstract-opdef-credential-record-type) of the [credential record](#credential-record).

`id`

The [id](#abstract-opdef-credential-record-id) of the [credential record](#credential-record).

`transports`

The [transports](#abstract-opdef-credential-record-transports) of the [credential record](#credential-record).

Generating Authenticator

The Generating Authenticator is the authenticator involved in the [authenticatorMakeCredential](#authenticatormakecredential) operation resulting in the creation of a given [public key credential source](#public-key-credential-source). The [generating authenticator](#generating-authenticator) is the same as the [managing authenticator](#public-key-credential-source-managing-authenticator) for [single-device credentials](#single-device-credential). For [multi-device credentials](#multi-device-credential), the generating authenticator may or may not be the same as the current [managing authenticator](#public-key-credential-source-managing-authenticator) participating in a given [authentication](#authentication) operation.

Human Palatability

An identifier that is [human-palatable](#human-palatability) is intended to be rememberable and reproducible by typical human users, in contrast to identifiers that are, for example, randomly generated sequences of bits [\[EduPersonObjectClassSpec\]](#biblio-edupersonobjectclassspec "EduPerson").

Non-Discoverable Credential

This is a [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) whose [credential ID](#credential-id) must be provided in `allowCredentials` when calling `navigator.credentials.get()` because it is not [client-side discoverable](#client-side-discoverable-credential). See also [server-side credentials](#server-side-credential).

Registrable Origin Label

The first [domain label](https://url.spec.whatwg.org/#domain-label) of the [registrable domain](https://url.spec.whatwg.org/#host-registrable-domain) of a [domain](https://url.spec.whatwg.org/#concept-domain), or null if the [registrable domain](https://url.spec.whatwg.org/#host-registrable-domain) is null. For example, the [registrable origin label](#registrable-origin-label) of both `example.co.uk` and `www.example.de` is `example` if both `co.uk` and `de` are [public suffixes](https://url.spec.whatwg.org/#host-public-suffix).

Public Key Credential

Generically, a *credential* is data one entity presents to another in order to *authenticate* the former to the latter [\[RFC4949\]](#biblio-rfc4949 "Internet Security Glossary, Version 2"). The term [public key credential](#public-key-credential) refers to one of: a [public key credential source](#public-key-credential-source), the possibly- [attested](#attestation) [credential public key](#credential-public-key) corresponding to a [public key credential source](#public-key-credential-source), or an [authentication assertion](#authentication-assertion). Which one is generally determined by context.

Note: This is a [willful violation](https://infra.spec.whatwg.org/#willful-violation) of [\[RFC4949\]](#biblio-rfc4949 "Internet Security Glossary, Version 2"). In English, a "credential" is both a) the thing presented to prove a statement and b) intended to be used multiple times. It’s impossible to achieve both criteria securely with a single piece of data in a public key system. [\[RFC4949\]](#biblio-rfc4949 "Internet Security Glossary, Version 2") chooses to define a credential as the thing that can be used multiple times (the public key), while this specification gives "credential" the English term’s flexibility. This specification uses more specific terms to identify the data related to an [\[RFC4949\]](#biblio-rfc4949 "Internet Security Glossary, Version 2") credential:

"Authentication information" (possibly including a private key)

[Public key credential source](#public-key-credential-source)

"Signed value"

[Authentication assertion](#authentication-assertion)

[\[RFC4949\]](#biblio-rfc4949 "Internet Security Glossary, Version 2") "credential"

[Credential public key](#credential-public-key) or [attestation object](#attestation-object)

At [registration](#registration) time, the [authenticator](#authenticator) creates an asymmetric key pair, and stores its [private key portion](#credential-private-key) and information from the [Relying Party](#relying-party) into a [public key credential source](#public-key-credential-source). The [public key portion](#credential-public-key) is returned to the [Relying Party](#relying-party), which then stores it in the active [user account](#user-account). Subsequently, only that [Relying Party](#relying-party), as identified by its [RP ID](#rp-id), is able to employ the [public key credential](#public-key-credential) in [authentication ceremonies](#authentication), via the `get()` method. The [Relying Party](#relying-party) uses its stored copy of the [credential public key](#credential-public-key) to verify the resultant [authentication assertion](#authentication-assertion).

Public Key Credential Source

A [credential source](https://w3c.github.io/webappsec-credential-management/#credential-source) ([\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1")) used by an [authenticator](#authenticator) to generate [authentication assertions](#authentication-assertion). A [public key credential source](#public-key-credential-source) consists of a [struct](https://infra.spec.whatwg.org/#struct) with the following [items](https://infra.spec.whatwg.org/#struct-item):

type

whose value is of `PublicKeyCredentialType`, defaulting to `public-key`.

id

A [Credential ID](#credential-id).

privateKey

The [credential private key](#credential-private-key).

rpId

The [Relying Party Identifier](#relying-party-identifier), for the [Relying Party](#relying-party) this [public key credential source](#public-key-credential-source) is [scoped](#scope) to. This is determined by the `` `rp`.`id` `` parameter of the `create()` operation.

userHandle

The [user handle](#user-handle) associated when this [public key credential source](#public-key-credential-source) was created. This [item](https://infra.spec.whatwg.org/#struct-item) is nullable, however [user handle](#user-handle) MUST always be populated for [discoverable credentials](#discoverable-credential).

otherUI

OPTIONAL other information used by the [authenticator](#authenticator) to inform its UI. For example, this might include the user’s `displayName`. [otherUI](#public-key-credential-source-otherui) is a mutable item and SHOULD NOT be bound to the [public key credential source](#public-key-credential-source) in a way that prevents [otherUI](#public-key-credential-source-otherui) from being updated.

The [authenticatorMakeCredential](#authenticatormakecredential) operation creates a [public key credential source](#public-key-credential-source) [bound](#bound-credential) to a managing authenticator and returns the [credential public key](#credential-public-key) associated with its [credential private key](#credential-private-key). The [Relying Party](#relying-party) can use this [credential public key](#credential-public-key) to verify the [authentication assertions](#authentication-assertion) created by this [public key credential source](#public-key-credential-source).

Rate Limiting

The process (also known as throttling) by which an authenticator implements controls against brute force attacks by limiting the number of consecutive failed authentication attempts within a given period of time. If the limit is reached, the authenticator should impose a delay that increases exponentially with each successive attempt, or disable the current authentication modality and offer a different [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) if available. [Rate limiting](#rate-limiting) is often implemented as an aspect of [user verification](#user-verification).

Registration

Registration Ceremony

The [ceremony](#ceremony) where a user, a [Relying Party](#relying-party), and the user’s [client platform](#client-platform) (containing or connected to at least one [authenticator](#authenticator)) work in concert to create a [public key credential](#public-key-credential) and associate it with a [user account](#user-account). Note that this includes employing a [test of user presence](#test-of-user-presence) or [user verification](#user-verification). After a successful [registration ceremony](#registration-ceremony), the user can be authenticated by an [authentication ceremony](#authentication-ceremony).

The WebAuthn [registration ceremony](#registration-ceremony) is defined in [§ 7.1 Registering a New Credential](#sctn-registering-a-new-credential), and is initiated by the [Relying Party](#relying-party) invoking a `` `navigator.credentials.create()` `` operation with a `publicKey` argument. See [§ 5 Web Authentication API](#sctn-api) for an introductory overview and [§ 1.3.1 Registration](#sctn-sample-registration) for implementation examples.

Relying Party

WebAuthn Relying Party

The entity whose web application utilizes the [Web Authentication API](#sctn-api) to [register](#registration) and [authenticate](#authentication) users.

A [Relying Party](#relying-party) implementation typically consists of both some client-side script that invokes the [Web Authentication API](#web-authentication-api) in the [client](#client), and a server-side component that executes the [Relying Party operations](#sctn-rp-operations) and other application logic. Communication between the two components MUST use HTTPS or equivalent transport security, but is otherwise beyond the scope of this specification.

Note: While the term [Relying Party](#relying-party) is also often used in other contexts (e.g., X.509 and OAuth), an entity acting as a [Relying Party](#relying-party) in one context is not necessarily a [Relying Party](#relying-party) in other contexts. In this specification, the term [WebAuthn Relying Party](#webauthn-relying-party) is often shortened to be just [Relying Party](#relying-party), and explicitly refers to a [Relying Party](#relying-party) in the WebAuthn context. Note that in any concrete instantiation a WebAuthn context may be embedded in a broader overall context, e.g., one based on OAuth.

Relying Party Identifier

RP ID

In the context of the [WebAuthn API](#web-authentication-api), a [relying party identifier](#relying-party-identifier) is a [valid domain string](https://url.spec.whatwg.org/#valid-domain-string) identifying the [WebAuthn Relying Party](#webauthn-relying-party) on whose behalf a given [registration](#registration) or [authentication ceremony](#authentication) is being performed. A [public key credential](#public-key-credential) can only be used for [authentication](#authentication) with the same entity (as identified by [RP ID](#rp-id)) it was registered with.

By default, the [RP ID](#rp-id) for a WebAuthn operation is set to the caller’s [origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-origin) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain). This default MAY be overridden by the caller, as long as the caller-specified [RP ID](#rp-id) value [is a registrable domain suffix of or is equal to](https://html.spec.whatwg.org/multipage/browsers.html#is-a-registrable-domain-suffix-of-or-is-equal-to) the caller’s [origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-origin) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain). See also [§ 5.1.3 Create a New Credential - PublicKeyCredential’s \[\[Create\]\](origin, options, sameOriginWithAncestors) Internal Method](#sctn-createCredential) and [§ 5.1.4 Use an Existing Credential to Make an Assertion](#sctn-getAssertion).

An [RP ID](#rp-id) is based on a [host](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-host) ’s [domain](https://url.spec.whatwg.org/#concept-domain) name. It does not itself include a [scheme](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-scheme) or [port](https://url.spec.whatwg.org/#concept-url-port), as an [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) does. The [RP ID](#rp-id) of a [public key credential](#public-key-credential) determines its scope. I.e., it determines the set of origins on which the public key credential may be exercised, as follows:

- The [RP ID](#rp-id) must be equal to the [origin](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain), or a [registrable domain suffix](https://html.spec.whatwg.org/multipage/browsers.html#is-a-registrable-domain-suffix-of-or-is-equal-to) of the [origin](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain).
- One of the following must be true:
	- The [origin](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) ’s [scheme](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-scheme) is `https`.
		- The [origin](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) ’s [host](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-host) is `localhost` and its [scheme](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-scheme) is `http`.
- The [origin](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) ’s [port](https://url.spec.whatwg.org/#concept-url-port) is unrestricted.

For example, given a [Relying Party](#relying-party) whose origin is `https://login.example.com:1337`, then the following [RP ID](#rp-id) s are valid: `login.example.com` (default) and `example.com`, but not `m.login.example.com` and not `com`. Another example of a valid origin is `http://localhost:8000`, due to the origin being `localhost`.

This is done in order to match the behavior of pervasively deployed ambient credentials (e.g., cookies, [\[RFC6265\]](#biblio-rfc6265 "HTTP State Management Mechanism")). Please note that this is a greater relaxation of "same-origin" restrictions than what [document.domain](https://html.spec.whatwg.org/multipage/origin.html#dom-document-domain) ’s setter provides.

These restrictions on origin values apply to [WebAuthn Clients](#webauthn-client).

Other specifications mimicking the [WebAuthn API](#web-authentication-api) to enable WebAuthn [public key credentials](#public-key-credential) on non-Web platforms (e.g. native mobile applications), MAY define different rules for binding a caller to a [Relying Party Identifier](#relying-party-identifier). Though, the [RP ID](#rp-id) syntaxes MUST conform to either [valid domain strings](https://url.spec.whatwg.org/#valid-domain-string) or URIs [\[RFC3986\]](#biblio-rfc3986 "Uniform Resource Identifier (URI): Generic Syntax") [\[URL\]](#biblio-url "URL Standard").

Server-side Public Key Credential Source

Server-side Credential

\[DEPRECATED\] Non-Resident Credential

Note: Historically, [server-side credentials](#server-side-credential) have been known as [non-resident credentials](#non-resident-credential). For backwards compatibility purposes, the various [WebAuthn API](#web-authentication-api) and [Authenticator Model](#authenticator-model) components with various forms of `resident` within their names have not been changed.

A [Server-side Public Key Credential Source](#server-side-public-key-credential-source), or [Server-side Credential](#server-side-credential) for short, is a [public key credential source](#public-key-credential-source) that is only usable in an [authentication ceremony](#authentication-ceremony) when the [Relying Party](#relying-party) supplies its [credential ID](#credential-id) in `navigator.credentials.get()` ’s `allowCredentials` argument. This means that the [Relying Party](#relying-party) must manage the credential’s storage and discovery, as well as be able to first identify the user in order to discover the [credential IDs](#credential-id) to supply in the `navigator.credentials.get()` call.

[Client-side](#client-side) storage of the [public key credential source](#public-key-credential-source) is not required for a [server-side credential](#server-side-credential). This is in contrast to a [client-side discoverable credential](#client-side-discoverable-credential), which instead does not require the user to first be identified in order to provide the user’s [credential ID](#credential-id) s to a `navigator.credentials.get()` call.

See also: and [non-discoverable credential](#non-discoverable-credential).

Test of User Presence

A [test of user presence](#test-of-user-presence) is a simple form of [authorization gesture](#authorization-gesture) and technical process where a user interacts with an [authenticator](#authenticator) by (typically) simply touching it (other modalities may also exist), yielding a Boolean result. Note that this does not constitute [user verification](#user-verification) because a [user presence test](#test-of-user-presence), by definition, is not capable of [biometric recognition](#biometric-recognition), nor does it involve the presentation of a shared secret such as a password or PIN.

User Account

In the context of this specification, a [user account](#user-account) denotes the mapping of a set of [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential) [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1") to a (sub)set of a [Relying Party](#relying-party) ’s resources, as maintained and authorized by the [Relying Party](#relying-party). The [Relying Party](#relying-party) maps a given [public key credential](#public-key-credential) to a [user account](#user-account) by assigning a [user account](#user-account) -specific value to the credential’s [user handle](#user-handle) and storing a [credential record](#credential-record) for the credential in the [user account](#user-account). This mapping, the set of credentials, and their authorizations, may evolve over time. A given [user account](#user-account) might be accessed by one or more natural persons (also known as "users"), and one natural person might have access to one or more [user accounts](#user-account), depending on actions of the user(s) and the [Relying Party](#relying-party).

User consent means the user agrees with what they are being asked, i.e., it encompasses reading and understanding prompts. An [authorization gesture](#authorization-gesture) is a [ceremony](#ceremony) component often employed to indicate.

User Handle

A user handle is an identifier for a [user account](#user-account), specified by the [Relying Party](#relying-party) as `` `user`.`id` `` during [registration](#registration). [Discoverable credentials](#discoverable-credential) store this identifier and MUST return it as `` `response`.`userHandle` `` in [authentication ceremonies](#authentication-ceremony) started with an [empty](https://infra.spec.whatwg.org/#list-empty) `` `allowCredentials` `` argument.

The main use of the [user handle](#user-handle) is to identify the [user account](#user-account) in such [authentication ceremonies](#authentication-ceremony), but the [credential ID](#credential-id) could be used instead. The main differences are that the [credential ID](#credential-id) is chosen by the [authenticator](#authenticator) and is unique for each credential, while the [user handle](#user-handle) is chosen by the [Relying Party](#relying-party) and ought to be the same for all [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential) registered to the same [user account](#user-account).

[Authenticators](#authenticator) [map](#authenticator-credentials-map) pairs of [RP ID](#rp-id) and [user handle](#user-handle) to [public key credential sources](#public-key-credential-source). As a consequence, an authenticator will store at most one [discoverable credential](#discoverable-credential) per [user handle](#user-handle) per [Relying Party](#relying-party). Therefore a secondary use of the [user handle](#user-handle) is to allow [authenticators](#authenticator) to know when to replace an existing [discoverable credential](#discoverable-credential) with a new one during the [registration ceremony](#registration-ceremony).

A user handle is an opaque [byte sequence](https://infra.spec.whatwg.org/#byte-sequence) with a maximum size of 64 bytes, and is not meant to be displayed to the user. It MUST NOT contain personally identifying information, see [§ 14.6.1 User Handle Contents](#sctn-user-handle-privacy).

User Present

Upon successful completion of a [user presence test](#test-of-user-presence), the user is said to be " [present](#concept-user-present) ".

User Verification

The technical process by which an [authenticator](#authenticator) *locally authorizes* the invocation of the [authenticatorMakeCredential](#authenticatormakecredential) and [authenticatorGetAssertion](#authenticatorgetassertion) operations. [User verification](#user-verification) MAY be instigated through various [authorization gesture](#authorization-gesture) modalities; for example, through a touch plus pin code, password entry, or [biometric recognition](#biometric-recognition) (e.g., presenting a fingerprint) [\[ISOBiometricVocabulary\]](#biblio-isobiometricvocabulary "Information technology — Vocabulary — Biometrics"). The intent is to distinguish individual users. See also [§ 6.2.3 Authentication Factor Capability](#sctn-authentication-factor-capability).

Note that [user verification](#user-verification) does not give the [Relying Party](#relying-party) a concrete identification of the user, but when 2 or more ceremonies with [user verification](#user-verification) have been done with that [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) it expresses that it was the same user that performed all of them. The same user might not always be the same natural person, however, if multiple natural persons share access to the same [authenticator](#authenticator).

Note: Distinguishing natural persons depends in significant part upon the [client platform](#client-platform) ’s and [authenticator](#authenticator) ’s capabilities. For example, some devices are intended to be used by a single individual, yet they may allow multiple natural persons to enroll fingerprints or know the same PIN and thus access the same [user account](#user-account) (s) using that device.

NOTE: Invocation of the [authenticatorMakeCredential](#authenticatormakecredential) and [authenticatorGetAssertion](#authenticatorgetassertion) operations implies use of key material managed by the authenticator.

For security, [user verification](#user-verification) and use of [credential private keys](#credential-private-key) MUST all occur within the logical security boundary defining the [authenticator](#authenticator).

[User verification](#user-verification) procedures MAY implement [rate limiting](#rate-limiting) as a protection against brute force attacks.

User Verified

Upon successful completion of a [user verification](#user-verification) process, the user is said to be " [verified](#concept-user-verified) ".

## 5\. Web Authentication API

This section normatively specifies the API for creating and using [public key credentials](#public-key-credential). The basic idea is that the credentials belong to the user and are [managed](#public-key-credential-source-managing-authenticator) by a [WebAuthn Authenticator](#webauthn-authenticator), with which the [WebAuthn Relying Party](#webauthn-relying-party) interacts through the [client platform](#client-platform). [Relying Party](#relying-party) scripts can (with the ) request the browser to create a new credential for future use by the [Relying Party](#relying-party). See [Figure](#fig-registration) , below.

![](https://yubicolabs.github.io/webauthn-sign-extension/4/images/webauthn-registration-flow-01.svg)

Registration Flow

Scripts can also request the user’s permission to perform [authentication](#authentication) operations with an existing credential. See [Figure](#fig-authentication) , below.

![](https://yubicolabs.github.io/webauthn-sign-extension/4/images/webauthn-authentication-flow-01.svg)

Authentication Flow

All such operations are performed in the authenticator and are mediated by the [client platform](#client-platform) on the user’s behalf. At no point does the script get access to the credentials themselves; it only gets information about the credentials in the form of objects.

In addition to the above script interface, the authenticator MAY implement (or come with client software that implements) a user interface for management. Such an interface MAY be used, for example, to reset the authenticator to a clean state or to inspect the current state of the authenticator. In other words, such an interface is similar to the user interfaces provided by browsers for managing user state such as history, saved passwords, and cookies. Authenticator management actions such as credential deletion are considered to be the responsibility of such a user interface and are deliberately omitted from the API exposed to scripts.

The security properties of this API are provided by the client and the authenticator working together. The authenticator, which holds and [manages](#public-key-credential-source-managing-authenticator) credentials, ensures that all operations are [scoped](#scope) to a particular [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin), and cannot be replayed against a different [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin), by incorporating the [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) in its responses. Specifically, as defined in [§ 6.3 Authenticator Operations](#sctn-authenticator-ops), the full [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) of the requester is included, and signed over, in the [attestation object](#attestation-object) produced when a new credential is created as well as in all assertions produced by WebAuthn credentials.

Additionally, to maintain user privacy and prevent malicious [Relying Parties](#relying-party) from probing for the presence of [public key credentials](#public-key-credential) belonging to other [Relying Parties](#relying-party), each [credential](#public-key-credential) is also [scoped](#scope) to a [Relying Party Identifier](#relying-party-identifier), or [RP ID](#rp-id). This [RP ID](#rp-id) is provided by the client to the [authenticator](#authenticator) for all operations, and the [authenticator](#authenticator) ensures that [credentials](#public-key-credential) created by a [Relying Party](#relying-party) can only be used in operations requested by the same [RP ID](#rp-id). Separating the [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) from the [RP ID](#rp-id) in this way allows the API to be used in cases where a single [Relying Party](#relying-party) maintains multiple [origins](https://html.spec.whatwg.org/multipage/origin.html#concept-origin).

The client facilitates these security measures by providing the [Relying Party](#relying-party) ’s [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) and [RP ID](#rp-id) to the [authenticator](#authenticator) for each operation. Since this is an integral part of the WebAuthn security model, user agents only expose this API to callers in [secure contexts](https://html.spec.whatwg.org/multipage/webappapis.html#secure-context). For web contexts in particular, this only includes those accessed via a secure transport (e.g., TLS) established without errors.

The Web Authentication API is defined by the union of the Web IDL fragments presented in the following sections. A combined IDL listing is given in the [IDL Index](#idl-index).

### 5.1. PublicKeyCredential Interface

The `PublicKeyCredential` interface inherits from `Credential` [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1"), and contains the attributes that are returned to the caller when a new credential is created, or a new assertion is requested.

```
[SecureContext, Exposed=Window]
interface PublicKeyCredential : Credential {
    [SameObject] readonly attribute ArrayBuffer              rawId;
    [SameObject] readonly attribute AuthenticatorResponse    response;
    readonly attribute DOMString?                            authenticatorAttachment;
    AuthenticationExtensionsClientOutputs getClientExtensionResults();
    static Promise<boolean> isConditionalMediationAvailable();
    PublicKeyCredentialJSON toJSON();
};
```

`id`

This attribute is inherited from `Credential`, though `PublicKeyCredential` overrides `Credential` ’s getter, instead returning the [base64url encoding](#base64url-encoding) of the data contained in the object’s `[[identifier]]` [internal slot](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots).

`rawId`

This attribute returns the `ArrayBuffer` contained in the `[[identifier]]` internal slot.

`response`, of type [AuthenticatorResponse](#authenticatorresponse), readonly

This attribute contains the [authenticator](#authenticator) ’s response to the client’s request to either create a [public key credential](#public-key-credential), or generate an [authentication assertion](#authentication-assertion). If the `PublicKeyCredential` is created in response to `create()`, this attribute’s value will be an `AuthenticatorAttestationResponse`, otherwise, the `PublicKeyCredential` was created in response to `get()`, and this attribute’s value will be an `AuthenticatorAssertionResponse`.

`authenticatorAttachment`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString), readonly, nullable

This attribute reports the in effect at the time the `navigator.credentials.create()` or `navigator.credentials.get()` methods successfully complete. The attribute’s value SHOULD be a member of `AuthenticatorAttachment`. [Relying Parties](#relying-party) SHOULD treat unknown values as if the value were null.

Note: If, as the result of a [registration](#registration-ceremony) or [authentication ceremony](#authentication-ceremony), `authenticatorAttachment` ’s value is "cross-platform" and concurrently `isUserVerifyingPlatformAuthenticatorAvailable` returns `true`, then the user employed a [roaming authenticator](#roaming-authenticators) for this [ceremony](#ceremony) while there is an available [platform authenticator](#platform-authenticators). Thus the [Relying Party](#relying-party) has the opportunity to prompt the user to register the available [platform authenticator](#platform-authenticators), which may enable more streamlined user experience flows.

An [authenticator’s](#authenticator) could change over time. For example, a mobile phone might at one time only support [platform attachment](#platform-attachment) but later receive updates to support [cross-platform attachment](#cross-platform-attachment) as well.

`getClientExtensionResults()`

This operation returns the value of `[[clientExtensionsResults]]`, which is a [map](https://infra.spec.whatwg.org/#ordered-map) containing [extension identifier](#extension-identifier) → [client extension output](#client-extension-output) entries produced by the extension’s [client extension processing](#client-extension-processing).

`isConditionalMediationAvailable()`

`PublicKeyCredential` overrides this method to indicate availability for `conditional` mediation during `navigator.credentials.get()`. [WebAuthn Relying Parties](#webauthn-relying-party) SHOULD verify availability before attempting to set `` options.`mediation` `` to `conditional`.

Upon invocation, a promise is returned that resolves with a value of `true` if `conditional` [user mediation](https://w3c.github.io/webappsec-credential-management/#user-mediated) is available, or `false` otherwise.

This method has no arguments and returns a promise to a Boolean value.

The `conditionalGet` capability is equivalent to this promise resolving to `true`.

Note: If this method is not present, `conditional` [user mediation](https://w3c.github.io/webappsec-credential-management/#user-mediated) is not available for `navigator.credentials.get()`.

Note: This method does *not* indicate whether or not `conditional` [user mediation](https://w3c.github.io/webappsec-credential-management/#user-mediated) is available in `navigator.credentials.create()`. For that, see the `conditionalCreate` capability in `getClientCapabilities()`.

`toJSON()`

This operation returns `RegistrationResponseJSON` or `AuthenticationResponseJSON`, which are [JSON type](https://webidl.spec.whatwg.org/#dfn-json-types) representations mirroring `PublicKeyCredential`, suitable for submission to a [Relying Party](#relying-party) server as an `application/json` payload. The [client](#client) is in charge of [serializing values to JSON types as usual](https://webidl.spec.whatwg.org/#idl-tojson-operation), but MUST take additional steps to first encode any `ArrayBuffer` values to `DOMString` values using [base64url encoding](#base64url-encoding).

The `RegistrationResponseJSON.clientExtensionResults` or `AuthenticationResponseJSON.clientExtensionResults` member MUST be set to the output of `getClientExtensionResults()`, with any `ArrayBuffer` values encoded to `DOMString` values using [base64url encoding](#base64url-encoding). This MAY include `ArrayBuffer` values from extensions registered in the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") but not defined in [§ 9 WebAuthn Extensions](#sctn-extensions).

The `AuthenticatorAttestationResponseJSON.transports` member MUST be set to the output of `getTransports()`.

The `AuthenticatorAttestationResponseJSON.publicKey` member MUST be set to the output of `getPublicKey()`.

The `AuthenticatorAttestationResponseJSON.publicKeyAlgorithm` member MUST be set to the output of `getPublicKeyAlgorithm()`.

```
typedef DOMString Base64URLString;
// The structure of this object will be either
// RegistrationResponseJSON or AuthenticationResponseJSON
typedef object PublicKeyCredentialJSON;

dictionary RegistrationResponseJSON {
    required DOMString id;
    required Base64URLString rawId;
    required AuthenticatorAttestationResponseJSON response;
    DOMString authenticatorAttachment;
    required AuthenticationExtensionsClientOutputsJSON clientExtensionResults;
    required DOMString type;
};

dictionary AuthenticatorAttestationResponseJSON {
    required Base64URLString clientDataJSON;
    required Base64URLString authenticatorData;
    required sequence<DOMString> transports;
    // The publicKey field will be missing if pubKeyCredParams was used to
    // negotiate a public-key algorithm that the user agent doesn't
    // understand. (See section “Easily accessing credential data” for a
    // list of which algorithms user agents must support.) If using such an
    // algorithm then the public key must be parsed directly from
    // attestationObject or authenticatorData.
    Base64URLString publicKey;
    required COSEAlgorithmIdentifier publicKeyAlgorithm;
    // This value contains copies of some of the fields above. See
    // section “Easily accessing credential data”.
    required Base64URLString attestationObject;
};

dictionary AuthenticationResponseJSON {
    required DOMString id;
    required Base64URLString rawId;
    required AuthenticatorAssertionResponseJSON response;
    DOMString authenticatorAttachment;
    required AuthenticationExtensionsClientOutputsJSON clientExtensionResults;
    required DOMString type;
};

dictionary AuthenticatorAssertionResponseJSON {
    required Base64URLString clientDataJSON;
    required Base64URLString authenticatorData;
    required Base64URLString signature;
    Base64URLString userHandle;
};

dictionary AuthenticationExtensionsClientOutputsJSON {
};
```

`[[type]]`

The `PublicKeyCredential` [interface object](https://webidl.spec.whatwg.org/#dfn-interface-object) ’s `[[type]]` [internal slot](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) ’s value is the string " `public-key` ".

Note: This is reflected via the `type` attribute getter inherited from `Credential`.

`[[discovery]]`

The `PublicKeyCredential` [interface object](https://webidl.spec.whatwg.org/#dfn-interface-object) ’s `[[discovery]]` [internal slot](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) ’s value is " `remote` ".

`[[identifier]]`

This [internal slot](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) contains the [credential ID](#credential-id), chosen by the authenticator. The [credential ID](#credential-id) is used to look up credentials for use, and is therefore expected to be globally unique with high probability across all credentials of the same type, across all authenticators.

This API does not constrain the format of this identifier, except that it MUST NOT be longer than 1023 bytes and MUST be sufficient for the [authenticator](#authenticator) to uniquely select a key. For example, an authenticator without on-board storage may create identifiers containing a [credential private key](#credential-private-key) wrapped with a symmetric key that is burned into the authenticator.

`[[clientExtensionsResults]]`

This [internal slot](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) contains the results of processing client extensions requested by the [Relying Party](#relying-party) upon the [Relying Party](#relying-party) ’s invocation of either `navigator.credentials.create()` or `navigator.credentials.get()`.

`PublicKeyCredential` ’s [interface object](https://webidl.spec.whatwg.org/#dfn-interface-object) inherits `Credential` ’s implementation of `[[CollectFromCredentialStore]](origin, options, sameOriginWithAncestors)`, and defines its own implementation of each of `[[Create]](origin, options, sameOriginWithAncestors)`, `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)`, and `[[Store]](credential, sameOriginWithAncestors)`.

Calling `CredentialsContainer` ’s `preventSilentAccess()` method will have no effect on `PublicKeyCredential` credentials, since they always require user interaction.

#### 5.1.1. CredentialCreationOptions Dictionary Extension

To support registration via `navigator.credentials.create()`, this document extends the `CredentialCreationOptions` dictionary as follows:

```
partial dictionary CredentialCreationOptions {
    PublicKeyCredentialCreationOptions      publicKey;
};
```

#### 5.1.2. CredentialRequestOptions Dictionary Extension

To support obtaining assertions via `navigator.credentials.get()`, this document extends the `CredentialRequestOptions` dictionary as follows:

```
partial dictionary CredentialRequestOptions {
    PublicKeyCredentialRequestOptions      publicKey;
};
```

#### 5.1.3. Create a New Credential - PublicKeyCredential’s \[\[Create\]\](origin, options, sameOriginWithAncestors) Internal Method

`PublicKeyCredential` ’s [interface object](https://webidl.spec.whatwg.org/#dfn-interface-object) ’s implementation of the `[[Create]](origin, options, sameOriginWithAncestors)` [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1") allows [WebAuthn Relying Party](#webauthn-relying-party) scripts to call `navigator.credentials.create()` to request the creation of a new [public key credential source](#public-key-credential-source), [bound](#bound-credential) to an [authenticator](#authenticator).

By setting `` options.`mediation` `` to `conditional`, [Relying Parties](#relying-party) can indicate that they would like to register a credential without prominent modal UI if the user has already consented to create a credential. The [Relying Party](#relying-party) SHOULD first use `getClientCapabilities()` to check that the [client](#client) supports the `conditionalCreate` capability in order to prevent a user-visible error in case this feature is not available. The client MUST set BOTH requireUserPresence and requireUserVerification to FALSE when `` options.`mediation` `` is set to `conditional` unless they may be explicitly performed during the ceremony.

Any `navigator.credentials.create()` operation can be aborted by leveraging the `AbortController`; see [DOM § 3.3 Using AbortController and AbortSignal objects in APIs](https://dom.spec.whatwg.org/#abortcontroller-api-integration) for detailed instructions.

This [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) accepts three arguments:

`origin`

This argument is the ’s [origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-origin), as determined by the calling `create()` implementation.

`options`

This argument is a `CredentialCreationOptions` object whose `` options.`publicKey` `` member contains a `PublicKeyCredentialCreationOptions` object specifying the desired attributes of the to-be-created [public key credential](#public-key-credential).

`sameOriginWithAncestors`

This argument is a Boolean value which is `true` if and only if the caller’s [environment settings object](https://html.spec.whatwg.org/multipage/webappapis.html#environment-settings-object) is [same-origin with its ancestors](https://w3c.github.io/webappsec-credential-management/#same-origin-with-its-ancestors). It is `false` if caller is cross-origin.

Note: Invocation of this [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) indicates that it was allowed by [permissions policy](https://html.spec.whatwg.org/multipage/dom.html#concept-document-permissions-policy), which is evaluated at the [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1") level. See [§ 5.9 Permissions Policy integration](#sctn-permissions-policy).

Note: **This algorithm is synchronous:** the `Promise` resolution/rejection is handled by `navigator.credentials.create()`.

All `BufferSource` objects used in this algorithm MUST be snapshotted when the algorithm begins, to avoid potential synchronization issues. Implementations SHOULD [get a copy of the bytes held by the buffer source](https://webidl.spec.whatwg.org/#dfn-get-buffer-source-copy) and use that copy for relevant portions of the algorithm.

When this method is invoked, the user agent MUST execute the following algorithm:

1. Assert: `` options.`publicKey` `` is present.
2. If sameOriginWithAncestors is `false`:
	1. If `` options.`mediation` `` is present with the value `conditional`:
		1. Throw a " `NotAllowedError` " `DOMException`
		2. If the, as determined by the calling `create()` implementation, does not have [transient activation](https://html.spec.whatwg.org/multipage/interaction.html#transient-activation):
		1. Throw a " `NotAllowedError` " `DOMException`.
		3. [Consume user activation](https://html.spec.whatwg.org/multipage/interaction.html#consume-user-activation) of the.
		4. If the [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) that is creating a credential is different from the [top-level origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-environment-top-level-origin) of the (i.e., is a different origin than the user can see in the address bar), the [client](#client) SHOULD make this fact clear to the user.
3. Let pkOptions be the value of `` options.`publicKey` ``.
4. If `` pkOptions.`timeout` `` is present, check if its value lies within a reasonable range as defined by the [client](#client) and if not, correct it to the closest value lying within that range. Set a timer lifetimeTimer to this adjusted value. If `` pkOptions.`timeout` `` is not present, then set lifetimeTimer to a [client](#client) -specific default.
	See the for guidance on deciding a reasonable range and default for `` pkOptions.`timeout` ``.
	The [client](#client) SHOULD take cognitive guidelines into considerations regarding timeout for users with special needs.
5. If the length of `` pkOptions.`user`.`id` `` is not between 1 and 64 bytes (inclusive) then throw a `TypeError`.
6. Let callerOrigin be `origin`. If callerOrigin is an [opaque origin](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-opaque), throw a " `NotAllowedError` " `DOMException`.
7. Let effectiveDomain be the callerOrigin ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain). If [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain) is not a [valid domain](https://url.spec.whatwg.org/#valid-domain), then throw a " `SecurityError` " `DOMException`.
	Note: An [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain) may resolve to a [host](https://url.spec.whatwg.org/#concept-url-host), which can be represented in various manners, such as [domain](https://url.spec.whatwg.org/#concept-domain), [ipv4 address](https://url.spec.whatwg.org/#concept-ipv4), [ipv6 address](https://url.spec.whatwg.org/#concept-ipv6), [opaque host](https://url.spec.whatwg.org/#opaque-host), or [empty host](https://url.spec.whatwg.org/#empty-host). Only the [domain](https://url.spec.whatwg.org/#concept-domain) format of [host](https://url.spec.whatwg.org/#concept-url-host) is allowed here. This is for simplification and also is in recognition of various issues with using direct IP address identification in concert with PKI-based security.
8. If `` pkOptions.`rp`.`id` ``
	is present
	If `` pkOptions.`rp`.`id` `` [is not a registrable domain suffix of and is not equal to](https://html.spec.whatwg.org/multipage/browsers.html#is-a-registrable-domain-suffix-of-or-is-equal-to) effectiveDomain, and if the client
	supports [related origin requests](#sctn-related-origins)
	1. Let rpIdRequested be the value of `` pkOptions.`rp`.`id` ``.
	2. Run the with arguments callerOrigin and rpIdRequested. If the result is `false`, throw a " `SecurityError` " `DOMException`.
	does not support [related origin requests](#sctn-related-origins)
	throw a " `SecurityError` " `DOMException`.
	is not present
	Set `` pkOptions.`rp`.`id` `` to effectiveDomain.
	Note: `` pkOptions.`rp`.`id` `` represents the caller’s [RP ID](#rp-id). The [RP ID](#rp-id) defaults to being the caller’s [origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-origin) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain) unless the caller has explicitly set `` pkOptions.`rp`.`id` `` when calling `create()`.
9. Let credTypesAndPubKeyAlgs be a new [list](https://infra.spec.whatwg.org/#list) whose [items](https://infra.spec.whatwg.org/#list-item) are pairs of `PublicKeyCredentialType` and a `COSEAlgorithmIdentifier`.
10. If `` pkOptions.`pubKeyCredParams` `` ’s [size](https://infra.spec.whatwg.org/#list-size)
	is zero
	[Append](https://infra.spec.whatwg.org/#list-append) the following pairs of `PublicKeyCredentialType` and `COSEAlgorithmIdentifier` values to credTypesAndPubKeyAlgs:
	- `public-key` and `-7` ("ES256").
	- `public-key` and `-257` ("RS256").
	is non-zero
	[For each](https://infra.spec.whatwg.org/#list-iterate) current of `` pkOptions.`pubKeyCredParams` ``:
	1. If `` current.`type` `` does not contain a `PublicKeyCredentialType` supported by this implementation, then [continue](https://infra.spec.whatwg.org/#iteration-continue).
	2. Let alg be `` current.`alg` ``.
	3. [Append](https://infra.spec.whatwg.org/#list-append) the pair of `` current.`type` `` and alg to credTypesAndPubKeyAlgs.
	If credTypesAndPubKeyAlgs [is empty](https://infra.spec.whatwg.org/#list-is-empty), throw a " `NotSupportedError` " `DOMException`.
11. Let clientExtensions be a new [map](https://infra.spec.whatwg.org/#ordered-map) and let authenticatorExtensions be a new [map](https://infra.spec.whatwg.org/#ordered-map).
12. If `` pkOptions.`extensions` `` is present, then [for each](https://infra.spec.whatwg.org/#map-iterate) extensionId → clientExtensionInput of `` pkOptions.`extensions` ``:
	1. If extensionId is not supported by this [client platform](#client-platform) or is not a [registration extension](#registration-extension), then [continue](https://infra.spec.whatwg.org/#iteration-continue).
		2. [Set](https://infra.spec.whatwg.org/#map-set) clientExtensions \[extensionId\] to clientExtensionInput.
		3. If extensionId is not an [authenticator extension](#authenticator-extension), then [continue](https://infra.spec.whatwg.org/#iteration-continue).
		4. Let authenticatorExtensionInput be the ([CBOR](#cbor)) result of running extensionId ’s [client extension processing](#client-extension-processing) algorithm on clientExtensionInput. If the algorithm returned an error, [continue](https://infra.spec.whatwg.org/#iteration-continue).
		5. [Set](https://infra.spec.whatwg.org/#map-set) authenticatorExtensions \[extensionId\] to the [base64url encoding](#base64url-encoding) of authenticatorExtensionInput.
13. Let collectedClientData be a new `CollectedClientData` instance whose fields are:
	`type`
	The string "webauthn.create".
	`challenge`
	The [base64url encoding](#base64url-encoding) of pkOptions.`challenge`.
	`origin`
	The [serialization of](https://html.spec.whatwg.org/multipage/browsers.html#ascii-serialisation-of-an-origin) callerOrigin.
	`crossOrigin`
	The inverse of the value of the `sameOriginWithAncestors` argument passed to this [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots).
	`topOrigin`
	The [serialization of](https://html.spec.whatwg.org/multipage/browsers.html#ascii-serialisation-of-an-origin) callerOrigin ’s [top-level origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-environment-top-level-origin) if the `sameOriginWithAncestors` argument passed to this [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) is `false`, else `undefined`.
14. Let clientDataJSON be the [JSON-compatible serialization of client data](#collectedclientdata-json-compatible-serialization-of-client-data) constructed from collectedClientData.
15. Let clientDataHash be the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data) represented by clientDataJSON.
16. If `` options.`signal` `` is present and [aborted](https://dom.spec.whatwg.org/#abortsignal-aborted), throw the `` options.`signal` `` ’s [abort reason](https://dom.spec.whatwg.org/#abortsignal-abort-reason).
17. Let issuedRequests be a new [ordered set](https://infra.spec.whatwg.org/#ordered-set).
18. Let authenticators represent a value which at any given instant is a [set](https://infra.spec.whatwg.org/#ordered-set) of [client platform](#client-platform) -specific handles, where each [item](https://infra.spec.whatwg.org/#list-item) identifies an [authenticator](#authenticator) presently available on this [client platform](#client-platform) at that instant.
	Note: What qualifies an [authenticator](#authenticator) as "available" is intentionally unspecified; this is meant to represent how [authenticators](#authenticator) can be [hot-plugged](https://en.wikipedia.org/w/index.php?title=Hot_plug) into (e.g., via USB) or discovered (e.g., via NFC or Bluetooth) by the [client](#client) by various mechanisms, or permanently built into the [client](#client).
19. If `` options.`mediation` `` is present with the value `conditional`:
	1. If the user agent has not recently mediated an authentication, the origin of said authentication is not callerOrigin, or the user does not consent to this type of credential creation, throw a " `NotAllowedError` " `DOMException`.
		It is up to the user agent to decide when it believes an authentication ceremony has been completed. That authentication ceremony MAY be performed via other means than the [Web Authentication API](#web-authentication-api).
20. Consider the value of `hints` and craft the user interface accordingly, as the user-agent sees fit.
21. Start lifetimeTimer.
22. [While](https://infra.spec.whatwg.org/#iteration-while) lifetimeTimer has not expired, perform the following actions depending upon lifetimeTimer, and the state and response [for each](https://infra.spec.whatwg.org/#list-iterate) authenticator in authenticators:
	If lifetimeTimer expires,
	[For each](https://infra.spec.whatwg.org/#list-iterate) authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	If the user exercises a user agent user-interface option to cancel the process,
	[For each](https://infra.spec.whatwg.org/#list-iterate) authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests. Throw a " `NotAllowedError` " `DOMException`.
	If `` options.`signal` `` is present and [aborted](https://dom.spec.whatwg.org/#abortsignal-aborted),
	[For each](https://infra.spec.whatwg.org/#list-iterate) authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests. Then throw the `` options.`signal` `` ’s [abort reason](https://dom.spec.whatwg.org/#abortsignal-abort-reason).
	If an authenticator becomes available on this [client device](#client-device),
	Note: This includes the case where an authenticator was available upon lifetimeTimer initiation.
	1. This authenticator is now the candidate authenticator.
	2. If `` pkOptions.`authenticatorSelection` `` is present:
		1. If `` pkOptions.`authenticatorSelection`.`authenticatorAttachment` `` is present and its value is not equal to authenticator ’s, [continue](https://infra.spec.whatwg.org/#iteration-continue).
			2. If `` pkOptions.`authenticatorSelection`.`residentKey` ``
			is present and set to `required`
			If the authenticator is not capable of storing a [client-side discoverable public key credential source](#client-side-discoverable-public-key-credential-source), [continue](https://infra.spec.whatwg.org/#iteration-continue).
			is present and set to `preferred` or `discouraged`
			No effect.
			is not present
			if `` pkOptions.`authenticatorSelection`.`requireResidentKey` `` is set to `true` and the authenticator is not capable of storing a [client-side discoverable public key credential source](#client-side-discoverable-public-key-credential-source), [continue](https://infra.spec.whatwg.org/#iteration-continue).
			3. If `` pkOptions.`authenticatorSelection`.`userVerification` `` is set to `required` and the authenticator is not capable of performing [user verification](#user-verification), [continue](https://infra.spec.whatwg.org/#iteration-continue).
	3. Let requireResidentKey be the effective resident key requirement for credential creation, a Boolean value, as follows:
		If `` pkOptions.`authenticatorSelection`.`residentKey` ``
		is present and set to `required`
		Let requireResidentKey be `true`.
		is present and set to `preferred`
		If the authenticator
		is capable of
		Let requireResidentKey be `true`.
		is not capable of, or if the [client](#client) cannot determine authenticator capability,
		Let requireResidentKey be `false`.
		is present and set to `discouraged`
		Let requireResidentKey be `false`.
		is not present
		Let requireResidentKey be the value of `` pkOptions.`authenticatorSelection`.`requireResidentKey` ``.
	4. Let userVerification be the effective user verification requirement for credential creation, a Boolean value, as follows. If `` pkOptions.`authenticatorSelection`.`userVerification` ``
		is set to `required`
		1. If `` options.`mediation` `` is set to `conditional` and [user verification](#user-verification) cannot be collected during the ceremony, throw a `ConstraintError` `DOMException`.
		2. Let userVerification be `true`.
		is set to `preferred`
		If the authenticator
		is capable of [user verification](#user-verification)
		Let userVerification be `true`.
		is not capable of [user verification](#user-verification)
		Let userVerification be `false`.
		is set to `discouraged`
		Let userVerification be `false`.
	5. Let enterpriseAttestationPossible be a Boolean value, as follows. If `` pkOptions.`attestation` ``
		is set to `enterprise`
		Let enterpriseAttestationPossible be `true` if the user agent wishes to support enterprise attestation for `` pkOptions.`rp`.`id` `` (see [step 8](#CreateCred-DetermineRpId), above). Otherwise `false`.
		otherwise
		Let enterpriseAttestationPossible be `false`.
	6. Let attestationFormats be a list of strings, initialized to the value of `` pkOptions.`attestationFormats` ``.
	7. If `` pkOptions.`attestation` ``
		is set to `none`
		Set attestationFormats be the single-element list containing the string “none”
	8. Let excludeCredentialDescriptorList be a new [list](https://infra.spec.whatwg.org/#list).
	9. [For each](https://infra.spec.whatwg.org/#list-iterate) credential descriptor C in `` pkOptions.`excludeCredentials` ``:
		1. If `` C.`transports` `` [is not empty](https://infra.spec.whatwg.org/#list-is-empty), and authenticator is connected over a transport not mentioned in `` C.`transports` ``, the client MAY [continue](https://infra.spec.whatwg.org/#iteration-continue).
			Note: If the client chooses to [continue](https://infra.spec.whatwg.org/#iteration-continue), this could result in inadvertently registering multiple credentials [bound to](#bound-credential) the same [authenticator](#authenticator) if the transport hints in `` C.`transports` `` are not accurate. For example, stored transport hints could become inaccurate as a result of software upgrades adding new connectivity options.
			2. Otherwise, [Append](https://infra.spec.whatwg.org/#list-append) C to excludeCredentialDescriptorList.
	10. Invoke the [authenticatorMakeCredential](#authenticatormakecredential) operation on authenticator with clientDataHash, `` pkOptions.`rp` ``, `` pkOptions.`user` ``, requireResidentKey, userVerification, credTypesAndPubKeyAlgs, excludeCredentialDescriptorList, enterpriseAttestationPossible, attestationFormats, and authenticatorExtensions as parameters.
	11. [Append](https://infra.spec.whatwg.org/#set-append) authenticator to issuedRequests.
	If an authenticator ceases to be available on this [client device](#client-device),
	[Remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	If any authenticator returns a status indicating that the user cancelled the operation,
	1. [Remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	2. [For each](https://infra.spec.whatwg.org/#list-iterate) remaining authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) it from issuedRequests.
		Note: [Authenticators](#authenticator) may return an indication of "the user cancelled the entire operation". How a user agent manifests this state to users is unspecified.
	If any authenticator returns an error status equivalent to " `InvalidStateError` ",
	1. [Remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	2. [For each](https://infra.spec.whatwg.org/#list-iterate) remaining authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) it from issuedRequests.
	3. Throw an " `InvalidStateError` " `DOMException`.
	Note: This error status is handled separately because the authenticator returns it only if excludeCredentialDescriptorList identifies a credential [bound](#bound-credential) to the authenticator and the user has to the operation. Given this explicit consent, it is acceptable for this case to be distinguishable to the [Relying Party](#relying-party).
	If any authenticator returns an error status not equivalent to " `InvalidStateError` ",
	[Remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	Note: This case does not imply for the operation, so details about the error are hidden from the [Relying Party](#relying-party) in order to prevent leak of potentially identifying information. See [§ 14.5.1 Registration Ceremony Privacy](#sctn-make-credential-privacy) for details.
	If any authenticator indicates success,
	1. [Remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests. This authenticator is now the selected authenticator.
	2. Let credentialCreationData be a [struct](https://infra.spec.whatwg.org/#struct) whose [items](https://infra.spec.whatwg.org/#struct-item) are:
		`attestationObjectResult`
		whose value is the bytes returned from the successful [authenticatorMakeCredential](#authenticatormakecredential) operation.
		Note: this value is `attObj`, as defined in [§ 6.5.4 Generating an Attestation Object](#sctn-generating-an-attestation-object).
		`clientDataJSONResult`
		whose value is the bytes of clientDataJSON.
		`attestationConveyancePreferenceOption`
		whose value is the value of pkOptions.`attestation`.
		`clientExtensionResults`
		whose value is an `AuthenticationExtensionsClientOutputs` object containing [extension identifier](#extension-identifier) → [client extension output](#client-extension-output) entries. The entries are created by running each extension’s [client extension processing](#client-extension-processing) algorithm to create the [client extension outputs](#client-extension-output), for each [client extension](#client-extension) in `` pkOptions.`extensions` ``.
	3. Let constructCredentialAlg be an algorithm that takes a [global object](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-global) global, and whose steps are:
		1. If `credentialCreationData.attestationConveyancePreferenceOption` ’s value is
			`none`
			Replace potentially uniquely identifying information with non-identifying versions of the same:
			1. If the [aaguid](#authdata-attestedcredentialdata-aaguid) in the [attested credential data](#attested-credential-data) is 16 zero bytes, `credentialCreationData.attestationObjectResult.fmt` is "packed", and "x5c" is absent from `credentialCreationData.attestationObjectResult`, then [self attestation](#self-attestation) is being used and no further action is needed.
			2. Otherwise:
				1. Set the value of `credentialCreationData.attestationObjectResult.fmt` to "none", and set the value of `credentialCreationData.attestationObjectResult.attStmt` to be an empty [CBOR](#cbor) map. (See [§ 8.7 None Attestation Statement Format](#sctn-none-attestation) and [§ 6.5.4 Generating an Attestation Object](#sctn-generating-an-attestation-object)).
			`indirect`
			The client MAY replace the [aaguid](#authdata-attestedcredentialdata-aaguid) and [attestation statement](#attestation-statement) with a more privacy-friendly and/or more easily verifiable version of the same data (for example, by employing an [Anonymization CA](#anonymization-ca)).
			`direct` or `enterprise`
			Convey the [authenticator](#authenticator) ’s [AAGUID](#aaguid) and [attestation statement](#attestation-statement), unaltered, to the [Relying Party](#relying-party).
			2. Let attestationObject be a new `ArrayBuffer`, created using global ’s [%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor), containing the bytes of `credentialCreationData.attestationObjectResult` ’s value.
			3. Let id be `attestationObject.authData.attestedCredentialData.credentialId`.
			4. Let pubKeyCred be a new `PublicKeyCredential` object associated with global whose fields are:
			`[[identifier]]`
			id
			`authenticatorAttachment`
			The `AuthenticatorAttachment` value matching the current of authenticator.
			`response`
			A new `AuthenticatorAttestationResponse` object associated with global whose fields are:
			`clientDataJSON`
			A new `ArrayBuffer`, created using global ’s [%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor), containing the bytes of `credentialCreationData.clientDataJSONResult`.
			`attestationObject`
			attestationObject
			`[[transports]]`
			A sequence of zero or more unique `DOMString` s, in lexicographical order, that the authenticator is believed to support. The values SHOULD be members of `AuthenticatorTransport`, but [client platforms](#client-platform) MUST ignore unknown values.
			If a user agent does not wish to divulge this information it MAY substitute an arbitrary sequence designed to preserve privacy. This sequence MUST still be valid, i.e. lexicographically sorted and free of duplicates. For example, it may use the empty sequence. Either way, in this case the user agent takes the risk that [Relying Party](#relying-party) behavior may be suboptimal.
			If the user agent does not have any transport information, it SHOULD set this field to the empty sequence.
			Note: How user agents discover transports supported by a given [authenticator](#authenticator) is outside the scope of this specification, but may include information from an [attestation certificate](#attestation-certificate) (for example [\[FIDO-Transports-Ext\]](#biblio-fido-transports-ext "FIDO U2F Authenticator Transports Extension")), metadata communicated in an [authenticator](#authenticator) protocol such as CTAP2, or special-case knowledge about a [platform authenticator](#platform-authenticators).
			`[[clientExtensionsResults]]`
			A new `ArrayBuffer`, created using global ’s [%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor), containing the bytes of `credentialCreationData.clientExtensionResults`.
			5. Return pubKeyCred.
	4. [For each](https://infra.spec.whatwg.org/#list-iterate) remaining authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) it from issuedRequests.
	5. Return constructCredentialAlg and terminate this algorithm.
23. Throw a " `NotAllowedError` " `DOMException`.

During the above process, the user agent SHOULD show some UI to the user to guide them in the process of selecting and authorizing an authenticator. When `` options.`mediation` `` is set to `conditional`, prominent modal UI should *not* be shown *unless* credential creation was previously consented to via means determined by the user agent.

##### 5.1.3.1. Create Request Exceptions

*This section is not normative.*

[WebAuthn Relying Parties](#webauthn-relying-party) can encounter a number of exceptions from a call to `navigator.credentials.create()`. Some exceptions can have multiple reasons for why they happened, requiring the [WebAuthn Relying Parties](#webauthn-relying-party) to infer the actual reason based on their use of WebAuthn.

Note: Exceptions that can be raised during processing of any [WebAuthn Extensions](#webauthn-extensions), including ones defined outside of this specification, are not listed here.

The following `DOMException` exceptions can be raised:

`AbortError`

The ceremony was cancelled by an `AbortController`. See [§ 5.6 Abort Operations with AbortSignal](#sctn-abortoperation) and [§ 1.3.4 Aborting Authentication Operations](#sctn-sample-aborting).

`ConstraintError`

Either `residentKey` was set to `required` and no available authenticator supported resident keys, or `userVerification` was set to `required` and no available authenticator could perform [user verification](#user-verification).

`InvalidStateError`

The authenticator used in the ceremony recognized an entry in `excludeCredentials` after the user to registering a credential.

`NotSupportedError`

No entry in `pubKeyCredParams` had a `type` property of `public-key`, or the [authenticator](#authenticator) did not support any of the signature algorithms specified in `pubKeyCredParams`.

`SecurityError`

The [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain) was not a [valid domain](https://url.spec.whatwg.org/#valid-domain), or `` `rp`.`id` `` was not equal to or a registrable domain suffix of the [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain). In the latter case, the [client](#client) does not support [related origin requests](#sctn-related-origins) or the failed.

`NotAllowedError`

A catch-all error covering a wide range of possible reasons, including common ones like the user canceling out of the ceremony. Some of these causes are documented throughout this spec, while others are client-specific.

The following [simple exceptions](https://webidl.spec.whatwg.org/#dfn-simple-exception) can be raised:

`TypeError`

The `options` argument was not a valid `CredentialCreationOptions` value, or the value of `` `user`.`id` `` was empty or was longer than 64 bytes.

#### 5.1.4. Use an Existing Credential to Make an Assertion

[WebAuthn Relying Parties](#webauthn-relying-party) call `navigator.credentials.get({publicKey:..., ...})` to discover and use an existing [public key credential](#public-key-credential), with the. [Relying Party](#relying-party) script optionally specifies some criteria to indicate what [public key credential sources](#public-key-credential-source) are acceptable to it. The [client platform](#client-platform) locates [public key credential sources](#public-key-credential-source) matching the specified criteria, and guides the user to pick one that the script will be allowed to use. The user may choose to decline the entire interaction even if a [public key credential source](#public-key-credential-source) is present, for example to maintain privacy. If the user picks a [public key credential source](#public-key-credential-source), the user agent then uses [§ 6.3.3 The authenticatorGetAssertion Operation](#sctn-op-get-assertion) to sign a [Relying Party](#relying-party) -provided challenge and other collected data into an [authentication assertion](#authentication-assertion), which is used as a [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential).

The `navigator.credentials.get()` implementation [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1") calls `` PublicKeyCredential.`[[CollectFromCredentialStore]]()` `` to collect any [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential) that should be available without [user mediation](https://w3c.github.io/webappsec-credential-management/#user-mediated) (roughly, this specification’s [authorization gesture](#authorization-gesture)), and if it does not find exactly one of those, it then calls `` PublicKeyCredential.`[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` `` to have the user select a [public key credential source](#public-key-credential-source).

Since this specification requires an [authorization gesture](#authorization-gesture) to create any [assertions](#assertion), `PublicKeyCredential` inherits the default behavior of `[[CollectFromCredentialStore]](origin, options, sameOriginWithAncestors)`, of returning an empty set. `PublicKeyCredential` ’s implementation of `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` is specified in the next section.

In general, the user agent SHOULD show some UI to the user to guide them in selecting and authorizing an authenticator with which to complete the operation. By setting `` options.`mediation` `` to `conditional`, [Relying Parties](#relying-party) can indicate that a prominent modal UI should *not* be shown *unless* credentials are discovered. The [Relying Party](#relying-party) SHOULD first use `isConditionalMediationAvailable()` or `getClientCapabilities()` to check that the [client](#client) supports the `conditionalGet` capability in order to prevent a user-visible error in case this feature is not available.

Any `navigator.credentials.get()` operation can be aborted by leveraging the `AbortController`; see [DOM § 3.3 Using AbortController and AbortSignal objects in APIs](https://dom.spec.whatwg.org/#abortcontroller-api-integration) for detailed instructions.

##### 5.1.4.1. PublicKeyCredential’s \[\[DiscoverFromExternalSource\]\](origin, options, sameOriginWithAncestors) Internal Method

This [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) accepts three arguments:

`origin`

This argument is the ’s [origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-origin), as determined by the calling `get()` implementation, i.e., `CredentialsContainer` ’s [Request a `Credential`](https://w3c.github.io/webappsec-credential-management/#abstract-opdef-request-a-credential) abstract operation.

`options`

This argument is a `CredentialRequestOptions` object whose `` options.`publicKey` `` member contains a `PublicKeyCredentialRequestOptions` object specifying the desired attributes of the [public key credential](#public-key-credential) to discover.

`sameOriginWithAncestors`

This argument is a Boolean value which is `true` if and only if the caller’s [environment settings object](https://html.spec.whatwg.org/multipage/webappapis.html#environment-settings-object) is [same-origin with its ancestors](https://w3c.github.io/webappsec-credential-management/#same-origin-with-its-ancestors). It is `false` if caller is cross-origin.

Note: Invocation of this [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) indicates that it was allowed by [permissions policy](https://html.spec.whatwg.org/multipage/dom.html#concept-document-permissions-policy), which is evaluated at the [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1") level. See [§ 5.9 Permissions Policy integration](#sctn-permissions-policy).

Note: **This algorithm is synchronous:** the `Promise` resolution/rejection is handled by `navigator.credentials.get()`.

All `BufferSource` objects used in this algorithm MUST be snapshotted when the algorithm begins, to avoid potential synchronization issues. Implementations SHOULD [get a copy of the bytes held by the buffer source](https://webidl.spec.whatwg.org/#dfn-get-buffer-source-copy) and use that copy for relevant portions of the algorithm.

When this method is invoked, the user agent MUST execute the following algorithm:

1. Assert: `` options.`publicKey` `` is present.
2. Let pkOptions be the value of `` options.`publicKey` ``.
3. If `` options.`mediation` `` is present with the value `conditional`:
	1. Let credentialIdFilter be the value of `` pkOptions.`allowCredentials` ``.
		2. Set `` pkOptions.`allowCredentials` `` to [empty](https://infra.spec.whatwg.org/#list-empty).
		Note: This prevents [non-discoverable credentials](#non-discoverable-credential) from being used during `conditional` requests.
		3. Set a timer lifetimeTimer to a value of infinity.
		Note: lifetimeTimer is set to a value of infinity so that the user has the entire lifetime of the [Document](https://dom.spec.whatwg.org/#concept-document) to interact with any `input` form control tagged with a `"webauthn"` [autofill detail token](https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens). For example, upon the user clicking in such an input field, the user agent can render a list of discovered credentials for the user to select from, and perhaps also give the user the option to "try another way".
4. Else:
	1. Let credentialIdFilter be an [empty](https://infra.spec.whatwg.org/#list-empty) [list](https://infra.spec.whatwg.org/#list).
		2. If `` pkOptions.`timeout` `` is present, check if its value lies within a reasonable range as defined by the [client](#client) and if not, correct it to the closest value lying within that range. Set a timer lifetimeTimer to this adjusted value. If `` pkOptions.`timeout` `` is not present, then set lifetimeTimer to a [client](#client) -specific default.
		See the for guidance on deciding a reasonable range and default for `` pkOptions.`timeout` ``.
		The user agent SHOULD take cognitive guidelines into considerations regarding timeout for users with special needs.
5. Let callerOrigin be `origin`. If callerOrigin is an [opaque origin](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-opaque), throw a " `NotAllowedError` " `DOMException`.
6. Let effectiveDomain be the callerOrigin ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain). If [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain) is not a [valid domain](https://url.spec.whatwg.org/#valid-domain), then throw a " `SecurityError` " `DOMException`.
	Note: An [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain) may resolve to a [host](https://url.spec.whatwg.org/#concept-url-host), which can be represented in various manners, such as [domain](https://url.spec.whatwg.org/#concept-domain), [ipv4 address](https://url.spec.whatwg.org/#concept-ipv4), [ipv6 address](https://url.spec.whatwg.org/#concept-ipv6), [opaque host](https://url.spec.whatwg.org/#opaque-host), or [empty host](https://url.spec.whatwg.org/#empty-host). Only the [domain](https://url.spec.whatwg.org/#concept-domain) format of [host](https://url.spec.whatwg.org/#concept-url-host) is allowed here. This is for simplification and also is in recognition of various issues with using direct IP address identification in concert with PKI-based security.
7. If `` pkOptions.`rpId` ``
	is present
	If `` pkOptions.`rpId` `` [is not a registrable domain suffix of and is not equal to](https://html.spec.whatwg.org/multipage/browsers.html#is-a-registrable-domain-suffix-of-or-is-equal-to) effectiveDomain, and if the client
	supports [related origin requests](#sctn-related-origins)
	1. Let rpIdRequested be the value of `` pkOptions.`rpId` ``
	2. Run the with arguments callerOrigin and rpIdRequested. If the result is `false`, throw a " `SecurityError` " `DOMException`.
	does not support [related origin requests](#sctn-related-origins)
	throw a " `SecurityError` " `DOMException`.
	is not present
	Set `` pkOptions.`rpId` `` to effectiveDomain.
	Note: rpId represents the caller’s [RP ID](#rp-id). The [RP ID](#rp-id) defaults to being the caller’s [origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-origin) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain) unless the caller has explicitly set `` pkOptions.`rpId` `` when calling `get()`.
8. Let clientExtensions be a new [map](https://infra.spec.whatwg.org/#ordered-map) and let authenticatorExtensions be a new [map](https://infra.spec.whatwg.org/#ordered-map).
9. If `` pkOptions.`extensions` `` is present, then [for each](https://infra.spec.whatwg.org/#map-iterate) extensionId → clientExtensionInput of `` pkOptions.`extensions` ``:
	1. If extensionId is not supported by this [client platform](#client-platform) or is not an [authentication extension](#authentication-extension), then [continue](https://infra.spec.whatwg.org/#iteration-continue).
		2. [Set](https://infra.spec.whatwg.org/#map-set) clientExtensions \[extensionId\] to clientExtensionInput.
		3. If extensionId is not an [authenticator extension](#authenticator-extension), then [continue](https://infra.spec.whatwg.org/#iteration-continue).
		4. Let authenticatorExtensionInput be the ([CBOR](#cbor)) result of running extensionId ’s [client extension processing](#client-extension-processing) algorithm on clientExtensionInput. If the algorithm returned an error, [continue](https://infra.spec.whatwg.org/#iteration-continue).
		5. [Set](https://infra.spec.whatwg.org/#map-set) authenticatorExtensions \[extensionId\] to the [base64url encoding](#base64url-encoding) of authenticatorExtensionInput.
10. Let collectedClientData be a new `CollectedClientData` instance whose fields are:
	`type`
	The string "webauthn.get".
	`challenge`
	The [base64url encoding](#base64url-encoding) of pkOptions.`challenge`
	`origin`
	The [serialization of](https://html.spec.whatwg.org/multipage/browsers.html#ascii-serialisation-of-an-origin) callerOrigin.
	`crossOrigin`
	The inverse of the value of the `sameOriginWithAncestors` argument passed to this [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots).
	`topOrigin`
	The [serialization of](https://html.spec.whatwg.org/multipage/browsers.html#ascii-serialisation-of-an-origin) callerOrigin ’s [top-level origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-environment-top-level-origin) if the `sameOriginWithAncestors` argument passed to this [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) is `false`, else `undefined`.
11. Let clientDataJSON be the [JSON-compatible serialization of client data](#collectedclientdata-json-compatible-serialization-of-client-data) constructed from collectedClientData.
12. Let clientDataHash be the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data) represented by clientDataJSON.
13. If `` options.`signal` `` is present and [aborted](https://dom.spec.whatwg.org/#abortsignal-aborted), throw the `` options.`signal` `` ’s [abort reason](https://dom.spec.whatwg.org/#abortsignal-abort-reason).
14. Let issuedRequests be a new [ordered set](https://infra.spec.whatwg.org/#ordered-set).
15. Let savedCredentialIds be a new [map](https://infra.spec.whatwg.org/#ordered-map).
16. Let authenticators represent a value which at any given instant is a [set](https://infra.spec.whatwg.org/#ordered-set) of [client platform](#client-platform) -specific handles, where each [item](https://infra.spec.whatwg.org/#list-item) identifies an [authenticator](#authenticator) presently available on this [client platform](#client-platform) at that instant.
	Note: What qualifies an [authenticator](#authenticator) as "available" is intentionally unspecified; this is meant to represent how [authenticators](#authenticator) can be [hot-plugged](https://en.wikipedia.org/w/index.php?title=Hot_plug) into (e.g., via USB) or discovered (e.g., via NFC or Bluetooth) by the [client](#client) by various mechanisms, or permanently built into the [client](#client).
17. Let silentlyDiscoveredCredentials be a new [map](https://infra.spec.whatwg.org/#ordered-map) whose [entries](https://infra.spec.whatwg.org/#map-entry) are of the form: → [authenticator](#authenticator).
18. Consider the value of `hints` and craft the user interface accordingly, as the user-agent sees fit.
19. Start lifetimeTimer.
20. [While](https://infra.spec.whatwg.org/#iteration-while) lifetimeTimer has not expired, perform the following actions depending upon lifetimeTimer, and the state and response [for each](https://infra.spec.whatwg.org/#list-iterate) authenticator in authenticators:
	If lifetimeTimer expires,
	[For each](https://infra.spec.whatwg.org/#list-iterate) authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	If the user exercises a user agent user-interface option to cancel the process,
	[For each](https://infra.spec.whatwg.org/#list-iterate) authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests. Throw a " `NotAllowedError` " `DOMException`.
	If `` options.`signal` `` is present and [aborted](https://dom.spec.whatwg.org/#abortsignal-aborted),
	[For each](https://infra.spec.whatwg.org/#list-iterate) authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests. Then throw the `` options.`signal` `` ’s [abort reason](https://dom.spec.whatwg.org/#abortsignal-abort-reason).
	If `` options.`mediation` `` is `conditional` and the user interacts with an `input` or `textarea` form control with an `autocomplete` attribute whose [non-autofill credential type](https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#non-autofill-credential-type) is `"webauthn"`,
	Note: The `"webauthn"` [autofill detail token](https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens) must appear immediately after the last [autofill detail token](https://html.spec.whatwg.org/multipage/form-control-infrastructure.html#autofill-detail-tokens) of type "Normal" or "Contact". For example:
	- `"username webauthn"`
	- `"current-password webauthn"`
	1. If silentlyDiscoveredCredentials is not [empty](https://infra.spec.whatwg.org/#list-empty):
		1. Prompt the user to optionally select a from silentlyDiscoveredCredentials. The prompt SHOULD display values from the of each, such as `name` and `displayName`.
			Let credentialMetadata be the chosen by the user, if any.
			2. If the user selects a credentialMetadata,
			1. Let publicKeyOptions be a temporary copy of pkOptions.
					2. Let authenticator be the value of silentlyDiscoveredCredentials \[credentialMetadata\].
					3. Set `` publicKeyOptions.`allowCredentials` `` to be a [list](https://infra.spec.whatwg.org/#list) containing a single `PublicKeyCredentialDescriptor` [item](https://infra.spec.whatwg.org/#list-item) whose `id` ’s value is set to credentialMetadata ’s ’s value and whose `type` value is set to credentialMetadata ’s.
					4. Execute the [issuing a credential request to an authenticator](#publickeycredential-issuing-a-credential-request-to-an-authenticator) algorithm with authenticator, savedCredentialIds, publicKeyOptions, rpId, clientDataHash, and authenticatorExtensions.
				If this returns `false`, [continue](https://infra.spec.whatwg.org/#iteration-continue).
					5. [Append](https://infra.spec.whatwg.org/#set-append) authenticator to issuedRequests.
	If `` options.`mediation` `` is not `conditional`, issuedRequests is empty, `` pkOptions.`allowCredentials` `` is not empty, and no authenticator will become available for any [public key credentials](#public-key-credential) therein,
	Indicate to the user that no eligible credential could be found. When the user acknowledges the dialog, throw a " `NotAllowedError` " `DOMException`.
	Note: One way a [client platform](#client-platform) can determine that no authenticator will become available is by examining the `` `transports` `` members of the present `` `PublicKeyCredentialDescriptor` `` [items](https://infra.spec.whatwg.org/#list-item) of `` pkOptions.`allowCredentials` ``, if any. For example, if all `` `PublicKeyCredentialDescriptor` `` [items](https://infra.spec.whatwg.org/#list-item) list only `` `internal` ``, but all [platform](#platform-authenticators) authenticator s have been tried, then there is no possibility of satisfying the request. Alternatively, all `` `PublicKeyCredentialDescriptor` `` [items](https://infra.spec.whatwg.org/#list-item) may list `` `transports` `` that the [client platform](#client-platform) does not support.
	If an authenticator becomes available on this [client device](#client-device),
	Note: This includes the case where an authenticator was available upon lifetimeTimer initiation.
	1. If `` options.`mediation` `` is `conditional` and the authenticator supports the [silentCredentialDiscovery](#silentcredentialdiscovery) operation:
		1. Let collectedDiscoveredCredentialMetadata be the result of invoking the [silentCredentialDiscovery](#silentcredentialdiscovery) operation on authenticator with rpId as parameter.
			2. [For each](https://infra.spec.whatwg.org/#list-iterate) credentialMetadata of collectedDiscoveredCredentialMetadata:
			1. If credentialIdFilter [is empty](https://infra.spec.whatwg.org/#list-is-empty) or credentialIdFilter contains an item whose `id` ’s value is set to credentialMetadata ’s, [set](https://infra.spec.whatwg.org/#map-set) silentlyDiscoveredCredentials \[credentialMetadata\] to authenticator.
				Note: A request will be issued to this authenticator upon user selection of a credential via interaction with a particular UI context (see [here](#GetAssn-ConditionalMediation-Interact-FormControl) for details).
	2. Else:
		1. Execute the [issuing a credential request to an authenticator](#publickeycredential-issuing-a-credential-request-to-an-authenticator) algorithm with authenticator, savedCredentialIds, pkOptions, rpId, clientDataHash, and authenticatorExtensions.
			If this returns `false`, [continue](https://infra.spec.whatwg.org/#iteration-continue).
			Note: This branch is taken if `` options.`mediation` `` is `conditional` and the authenticator does not support the [silentCredentialDiscovery](#silentcredentialdiscovery) operation to allow use of such authenticators during a `conditional` [user mediation](https://w3c.github.io/webappsec-credential-management/#user-mediated) request.
			2. [Append](https://infra.spec.whatwg.org/#set-append) authenticator to issuedRequests.
	If an authenticator ceases to be available on this [client device](#client-device),
	[Remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	If any authenticator returns a status indicating that the user cancelled the operation,
	1. [Remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	2. [For each](https://infra.spec.whatwg.org/#list-iterate) remaining authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) it from issuedRequests.
		Note: [Authenticators](#authenticator) may return an indication of "the user cancelled the entire operation". How a user agent manifests this state to users is unspecified.
	If any authenticator returns an error status,
	[Remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	If any authenticator indicates success,
	1. [Remove](https://infra.spec.whatwg.org/#list-remove) authenticator from issuedRequests.
	2. Let assertionCreationData be a [struct](https://infra.spec.whatwg.org/#struct) whose [items](https://infra.spec.whatwg.org/#struct-item) are:
		`credentialIdResult`
		If `savedCredentialIds[authenticator]` exists, set the value of [credentialIdResult](#assertioncreationdata-credentialidresult) to be the bytes of `savedCredentialIds[authenticator]`. Otherwise, set the value of [credentialIdResult](#assertioncreationdata-credentialidresult) to be the bytes of the [credential ID](#credential-id) returned from the successful [authenticatorGetAssertion](#authenticatorgetassertion) operation, as defined in [§ 6.3.3 The authenticatorGetAssertion Operation](#sctn-op-get-assertion).
		`clientDataJSONResult`
		whose value is the bytes of clientDataJSON.
		`authenticatorDataResult`
		whose value is the bytes of the [authenticator data](#authenticator-data) returned by the [authenticator](#authenticator).
		`signatureResult`
		whose value is the bytes of the signature value returned by the [authenticator](#authenticator).
		`userHandleResult`
		If the [authenticator](#authenticator) returned a [user handle](#user-handle), set the value of [userHandleResult](#assertioncreationdata-userhandleresult) to be the bytes of the returned [user handle](#user-handle). Otherwise, set the value of [userHandleResult](#assertioncreationdata-userhandleresult) to null.
		`clientExtensionResults`
		whose value is an `AuthenticationExtensionsClientOutputs` object containing [extension identifier](#extension-identifier) → [client extension output](#client-extension-output) entries. The entries are created by running each extension’s [client extension processing](#client-extension-processing) algorithm to create the [client extension outputs](#client-extension-output), for each [client extension](#client-extension) in `` pkOptions.`extensions` ``.
	3. If credentialIdFilter [is not empty](https://infra.spec.whatwg.org/#list-is-empty) and credentialIdFilter does not contain an item whose `id` ’s value is set to the value of [credentialIdResult](#assertioncreationdata-credentialidresult), [continue](https://infra.spec.whatwg.org/#iteration-continue).
	4. If credentialIdFilter [is empty](https://infra.spec.whatwg.org/#list-is-empty) and [userHandleResult](#assertioncreationdata-userhandleresult) is null, [continue](https://infra.spec.whatwg.org/#iteration-continue).
	5. Let settings be the [current settings object](https://html.spec.whatwg.org/multipage/webappapis.html#current-settings-object). Let global be settings ’ [global object](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-global).
	6. Let pubKeyCred be a new `PublicKeyCredential` object associated with global whose fields are:
		`[[identifier]]`
		A new `ArrayBuffer`, created using global ’s [%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor), containing the bytes of `assertionCreationData.credentialIdResult`.
		`authenticatorAttachment`
		The `AuthenticatorAttachment` value matching the current of authenticator.
		`response`
		A new `AuthenticatorAssertionResponse` object associated with global whose fields are:
		`clientDataJSON`
		A new `ArrayBuffer`, created using global ’s [%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor), containing the bytes of `assertionCreationData.clientDataJSONResult`.
		`authenticatorData`
		A new `ArrayBuffer`, created using global ’s [%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor), containing the bytes of `assertionCreationData.authenticatorDataResult`.
		`signature`
		A new `ArrayBuffer`, created using global ’s [%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor), containing the bytes of `assertionCreationData.signatureResult`.
		`userHandle`
		If `assertionCreationData.userHandleResult` is null, set this field to null. Otherwise, set this field to a new `ArrayBuffer`, created using global ’s [%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor), containing the bytes of `assertionCreationData.userHandleResult`.
		`[[clientExtensionsResults]]`
		A new `ArrayBuffer`, created using global ’s [%ArrayBuffer%](https://tc39.github.io/ecma262/#sec-arraybuffer-constructor), containing the bytes of `assertionCreationData.clientExtensionResults`.
	7. [For each](https://infra.spec.whatwg.org/#list-iterate) remaining authenticator in issuedRequests invoke the [authenticatorCancel](#authenticatorcancel) operation on authenticator and [remove](https://infra.spec.whatwg.org/#list-remove) it from issuedRequests.
	8. Return pubKeyCred and terminate this algorithm.
21. Throw a " `NotAllowedError` " `DOMException`.

##### 5.1.4.2. Issuing a Credential Request to an Authenticator

This sub-algorithm of `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` encompasses the specific UI context-independent steps necessary for requesting a [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) from a given [authenticator](#authenticator), using given `PublicKeyCredentialRequestOptions`. It is called by `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` from various points depending on which [user mediation](https://w3c.github.io/webappsec-credential-management/#user-mediated) the present [authentication ceremony](#authentication-ceremony) is subject to (e.g.: `conditional` mediation).

This algorithm accepts the following arguments:

`authenticator`

A [client platform](#client-platform) -specific handle identifying an [authenticator](#authenticator) presently available on this [client platform](#client-platform).

`savedCredentialIds`

A [map](https://infra.spec.whatwg.org/#ordered-map) containing [authenticator](#authenticator) → [credential ID](#credential-id). This argument will be modified in this algorithm.

`pkOptions`

This argument is a `PublicKeyCredentialRequestOptions` object specifying the desired attributes of the [public key credential](#public-key-credential) to discover.

`rpId`

The request [RP ID](#rp-id).

`clientDataHash`

The [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data) represented by clientDataJSON.

`authenticatorExtensions`

A [map](https://infra.spec.whatwg.org/#ordered-map) containing [extension identifiers](#extension-identifier) to the [base64url encoding](#base64url-encoding) of the [client extension processing](#client-extension-processing) output for [authenticator extensions](#authenticator-extension).

This algorithm returns `false` if the [client](#client) determines that the authenticator is not capable of handling the request, or `true` if the request was issued successfully.

The steps for [issuing a credential request to an authenticator](#publickeycredential-issuing-a-credential-request-to-an-authenticator) are as follows:

1. If `` pkOptions.`userVerification` `` is set to `required` and the authenticator is not capable of performing [user verification](#user-verification), return `false`.
2. Let userVerification be the effective user verification requirement for assertion, a Boolean value, as follows. If `` pkOptions.`userVerification` ``
	is set to `required`
	Let userVerification be `true`.
	is set to `preferred`
	If the authenticator
	is capable of [user verification](#user-verification)
	Let userVerification be `true`.
	is not capable of [user verification](#user-verification)
	Let userVerification be `false`.
	is set to `discouraged`
	Let userVerification be `false`.
3. If `` pkOptions.`allowCredentials` ``
	[is not empty](https://infra.spec.whatwg.org/#list-is-empty)
	1. Let allowCredentialDescriptorList be a new [list](https://infra.spec.whatwg.org/#list).
	2. Execute a [client platform](#client-platform) -specific procedure to determine which, if any, [public key credentials](#public-key-credential) described by `` pkOptions.`allowCredentials` `` are [bound](#bound-credential) to this authenticator, by matching with rpId, `` pkOptions.`allowCredentials`.`id` ``, and `` pkOptions.`allowCredentials`.`type` ``. Set allowCredentialDescriptorList to this filtered list.
	3. If allowCredentialDescriptorList [is empty](https://infra.spec.whatwg.org/#list-is-empty), return `false`.
	4. Let distinctTransports be a new [ordered set](https://infra.spec.whatwg.org/#ordered-set).
	5. If allowCredentialDescriptorList has exactly one value, set `savedCredentialIds[authenticator]` to `allowCredentialDescriptorList[0].id` ’s value (see [here](#authenticatorGetAssertion-return-values) in [§ 6.3.3 The authenticatorGetAssertion Operation](#sctn-op-get-assertion) for more information).
	6. [For each](https://infra.spec.whatwg.org/#list-iterate) credential descriptor C in allowCredentialDescriptorList, [append](https://infra.spec.whatwg.org/#set-append) each value, if any, of `` C.`transports` `` to distinctTransports.
		Note: This will aggregate only distinct values of `transports` (for this [authenticator](#authenticator)) in distinctTransports due to the properties of [ordered sets](https://infra.spec.whatwg.org/#ordered-set).
	7. If distinctTransports
		[is not empty](https://infra.spec.whatwg.org/#list-is-empty)
		The client selects one transport value from distinctTransports, possibly incorporating local configuration knowledge of the appropriate transport to use with authenticator in making its selection.
		Then, using transport, invoke the [authenticatorGetAssertion](#authenticatorgetassertion) operation on authenticator, with rpId, clientDataHash, allowCredentialDescriptorList, userVerification, and authenticatorExtensions as parameters.
		[is empty](https://infra.spec.whatwg.org/#list-is-empty)
		Using local configuration knowledge of the appropriate transport to use with authenticator, invoke the [authenticatorGetAssertion](#authenticatorgetassertion) operation on authenticator with rpId, clientDataHash, allowCredentialDescriptorList, userVerification, and authenticatorExtensions as parameters.
	[is empty](https://infra.spec.whatwg.org/#list-is-empty)
	Using local configuration knowledge of the appropriate transport to use with authenticator, invoke the [authenticatorGetAssertion](#authenticatorgetassertion) operation on authenticator with rpId, clientDataHash, userVerification, and authenticatorExtensions as parameters.
	Note: In this case, the [Relying Party](#relying-party) did not supply a list of acceptable credential descriptors. Thus, the authenticator is being asked to exercise any credential it may possess that is [scoped](#scope) to the [Relying Party](#relying-party), as identified by rpId.
4. Return `true`.

##### 5.1.4.3. Get Request Exceptions

*This section is not normative.*

[WebAuthn Relying Parties](#webauthn-relying-party) can encounter a number of exceptions from a call to `navigator.credentials.get()`. Some exceptions can have multiple reasons for why they happened, requiring the [WebAuthn Relying Parties](#webauthn-relying-party) to infer the actual reason based on their use of WebAuthn.

Note: Exceptions that can be raised during processing of any [WebAuthn Extensions](#webauthn-extensions), including ones defined outside of this specification, are not listed here.

The following `DOMException` exceptions can be raised:

`AbortError`

The ceremony was cancelled by an `AbortController`. See [§ 5.6 Abort Operations with AbortSignal](#sctn-abortoperation) and [§ 1.3.4 Aborting Authentication Operations](#sctn-sample-aborting).

`SecurityError`

The [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain) was not a [valid domain](https://url.spec.whatwg.org/#valid-domain), or `` `rp`.`id` `` was not equal to or a registrable domain suffix of the [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain). In the latter case, the [client](#client) does not support [related origin requests](#sctn-related-origins) or the failed.

`NotAllowedError`

A catch-all error covering a wide range of possible reasons, including common ones like the user canceling out of the ceremony. Some of these causes are documented throughout this spec, while others are client-specific.

The following [simple exceptions](https://webidl.spec.whatwg.org/#dfn-simple-exception) can be raised:

`TypeError`

The `options` argument was not a valid `CredentialRequestOptions` value.

#### 5.1.5. Store an Existing Credential - PublicKeyCredential’s \[\[Store\]\](credential, sameOriginWithAncestors) Internal Method

The `[[Store]](credential, sameOriginWithAncestors)` method is not supported for Web Authentication’s `PublicKeyCredential` type, so its implementation of the `[[Store]](credential, sameOriginWithAncestors)` [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) always throws an error.

Note: This algorithm is synchronous; the `Promise` resolution/rejection is handled by `navigator.credentials.store()`.

This [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) accepts two arguments:

`credential`

This argument is a `PublicKeyCredential` object.

`sameOriginWithAncestors`

This argument is a Boolean value which is `true` if and only if the caller’s [environment settings object](https://html.spec.whatwg.org/multipage/webappapis.html#environment-settings-object) is [same-origin with its ancestors](https://w3c.github.io/webappsec-credential-management/#same-origin-with-its-ancestors).

When this method is invoked, the user agent MUST execute the following algorithm:

1. Throw a " `NotSupportedError` " `DOMException`.

#### 5.1.6.

[WebAuthn Relying Parties](#webauthn-relying-party) use this method to determine whether they can create a new credential using a [user-verifying platform authenticator](#user-verifying-platform-authenticator). Upon invocation, the [client](#client) employs a [client platform](#client-platform) -specific procedure to discover available [user-verifying platform authenticators](#user-verifying-platform-authenticator). If any are discovered, the promise is resolved with the value of `true`. Otherwise, the promise is resolved with the value of `false`. Based on the result, the [Relying Party](#relying-party) can take further actions to guide the user to create a credential.

This method has no arguments and returns a Boolean value.

```
partial interface PublicKeyCredential {
    static Promise<boolean> isUserVerifyingPlatformAuthenticatorAvailable();
};
```

#### 5.1.7.

[WebAuthn Relying Parties](#webauthn-relying-party) use this method to determine the availability of a limited set of [client](#webauthn-client) capabilities to offer certain workflows and experiences to users. For example, an RP may offer a sign in button on clients where only `hybrid` transport is available or where `conditional` mediation is unavailable (instead of showing a username field).

Upon invocation, the [client](#client) employs a [client platform](#client-platform) -specific procedure to discover availablity of these capabilities.

This method has no arguments and returns a record of capability keys to Boolean values.

```
partial interface PublicKeyCredential {
    static Promise<PublicKeyCredentialClientCapabilities> getClientCapabilities();
};

typedef record<DOMString, boolean> PublicKeyCredentialClientCapabilities;
```

[Keys](https://infra.spec.whatwg.org/#map-getting-the-keys) in `PublicKeyCredentialClientCapabilities` MUST be sorted in ascending lexicographical order. The set of [keys](https://infra.spec.whatwg.org/#map-getting-the-keys) SHOULD contain the set of [enumeration values](https://webidl.spec.whatwg.org/#dfn-enumeration-value) of `ClientCapability`, but the client MAY omit keys as it deems necessary; see [§ 14.5.4 Disclosing Client Capabilities](#sctn-disclosing-client-capabilities).

When the value for a given capability is `true`, the feature is known to be currently supported by the client. When the value for a given capability is `false`, the feature is known to be not currently supported by the client. When a capability does not [exist](https://infra.spec.whatwg.org/#map-exists) as a key, the availability of the client feature is not known.

The set of [keys](https://infra.spec.whatwg.org/#map-getting-the-keys) SHOULD also contain a key for each [extension](#sctn-extensions) implemented by the client, where the key is formed by prefixing the string `extension:` to the [extension identifier](#extension-identifier). The associated value for each implemented extension SHOULD be `true`. If `getClientCapabilities()` is supported by a client, but an extension is not mapped to the value `true`, then a [Relying Party](#relying-party) MAY assume that client processing steps for that extension will not be carried out by this client and that the extension MAY not be forwarded to the authenticator.

Note that even if an extension is mapped to `true`, the authenticator used for any given operation may not support that extension, so [Relying Parties](#relying-party) MUST NOT assume that the authenticator processing steps for that extension will be performed on that basis.

#### 5.1.8. Deserialize Registration ceremony options - PublicKeyCredential’s parseCreationOptionsFromJSON() Method

[WebAuthn Relying Parties](#webauthn-relying-party) use this method to convert [JSON type](https://webidl.spec.whatwg.org/#dfn-json-types) representations of options for `navigator.credentials.create()` into `PublicKeyCredentialCreationOptions`.

Upon invocation, the [client](#client) MUST convert the `options` argument into a new, identically-structured `PublicKeyCredentialCreationOptions` object, using [base64url encoding](#base64url-encoding) to decode any `DOMString` attributes in `PublicKeyCredentialCreationOptionsJSON` that correspond to [buffer source type](https://webidl.spec.whatwg.org/#dfn-buffer-source-type) attributes in `PublicKeyCredentialCreationOptions`. This conversion MUST also apply to any [client extension inputs](#client-extension-input) processed by the [client](#client).

`AuthenticationExtensionsClientInputsJSON` MAY include extensions registered in the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") but not defined in [§ 9 WebAuthn Extensions](#sctn-extensions).

If the [client](#client) encounters any issues parsing any of the [JSON type](https://webidl.spec.whatwg.org/#dfn-json-types) representations then it MUST throw an " `EncodingError` " `DOMException` with a description of the incompatible value and terminate the operation.

```
partial interface PublicKeyCredential {
    static PublicKeyCredentialCreationOptions parseCreationOptionsFromJSON(PublicKeyCredentialCreationOptionsJSON options);
};

dictionary PublicKeyCredentialCreationOptionsJSON {
    required PublicKeyCredentialRpEntity                    rp;
    required PublicKeyCredentialUserEntityJSON              user;
    required Base64URLString                                challenge;
    required sequence<PublicKeyCredentialParameters>        pubKeyCredParams;
    unsigned long                                           timeout;
    sequence<PublicKeyCredentialDescriptorJSON>             excludeCredentials = [];
    AuthenticatorSelectionCriteria                          authenticatorSelection;
    sequence<DOMString>                                     hints = [];
    DOMString                                               attestation = "none";
    sequence<DOMString>                                     attestationFormats = [];
    AuthenticationExtensionsClientInputsJSON                extensions;
};

dictionary PublicKeyCredentialUserEntityJSON {
    required Base64URLString        id;
    required DOMString              name;
    required DOMString              displayName;
};

dictionary PublicKeyCredentialDescriptorJSON {
    required DOMString              type;
    required Base64URLString        id;
    sequence<DOMString>             transports;
};

dictionary AuthenticationExtensionsClientInputsJSON {
};
```

#### 5.1.9. Deserialize Authentication ceremony options - PublicKeyCredential’s parseRequestOptionsFromJSON() Methods

[WebAuthn Relying Parties](#webauthn-relying-party) use this method to convert [JSON type](https://webidl.spec.whatwg.org/#dfn-json-types) representations of options for `navigator.credentials.get()` into `PublicKeyCredentialRequestOptions`.

Upon invocation, the [client](#client) MUST convert the `options` argument into a new, identically-structured `PublicKeyCredentialRequestOptions` object, using [base64url encoding](#base64url-encoding) to decode any `DOMString` attributes in `PublicKeyCredentialRequestOptionsJSON` that correspond to [buffer source type](https://webidl.spec.whatwg.org/#dfn-buffer-source-type) attributes in `PublicKeyCredentialRequestOptions`. This conversion MUST also apply to any [client extension inputs](#client-extension-input) processed by the [client](#client).

`AuthenticationExtensionsClientInputsJSON` MAY include extensions registered in the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") but not defined in [§ 9 WebAuthn Extensions](#sctn-extensions).

If the [client](#client) encounters any issues parsing any of the [JSON type](https://webidl.spec.whatwg.org/#dfn-json-types) representations then it MUST throw an " `EncodingError` " `DOMException` with a description of the incompatible value and terminate the operation.

```
partial interface PublicKeyCredential {
    static PublicKeyCredentialRequestOptions parseRequestOptionsFromJSON(PublicKeyCredentialRequestOptionsJSON options);
};

dictionary PublicKeyCredentialRequestOptionsJSON {
    required Base64URLString                                challenge;
    unsigned long                                           timeout;
    DOMString                                               rpId;
    sequence<PublicKeyCredentialDescriptorJSON>             allowCredentials = [];
    DOMString                                               userVerification = "preferred";
    sequence<DOMString>                                     hints = [];
    AuthenticationExtensionsClientInputsJSON                extensions;
};
```

#### 5.1.10.

```
partial interface PublicKeyCredential {
    static Promise<undefined> signalUnknownCredential(UnknownCredentialOptions options);
    static Promise<undefined> signalAllAcceptedCredentials(AllAcceptedCredentialsOptions options);
    static Promise<undefined> signalCurrentUserDetails(CurrentUserDetailsOptions options);
};

dictionary UnknownCredentialOptions {
    required DOMString                     rpId;
    required Base64URLString               credentialId;
};

dictionary AllAcceptedCredentialsOptions {
    required DOMString                     rpId;
    required Base64URLString               userId;
    required sequence<Base64URLString>     allAcceptedCredentialIds;
};

dictionary CurrentUserDetailsOptions {
    required DOMString                     rpId;
    required Base64URLString               userId;
    required DOMString                     name;
    required DOMString                     displayName;
};
```

[WebAuthn Relying Parties](#webauthn-relying-party) may use these signal methods to inform [authenticators](#authenticator) of the state of [public key credentials](#public-key-credential), so that incorrect or revoked credentials may be updated, removed, or hidden. [Clients](#client) provide this functionality opportunistically, since an authenticator may not support updating its [credentials map](#authenticator-credentials-map) or may not be attached at the time the request is made. Furthermore, in order to avoid revealing information about a user’s credentials without, [signal methods](#signal-methods) do not indicate whether the operation succeeded. A successfully resolved promise only means that the `options` object was well formed.

Each [signal method](#signal-methods) includes authenticator actions. [Authenticators](#authenticator) MAY choose to deviate in their [authenticator actions](#signal-method-authenticator-actions) from the present specification, e.g. to ignore a change they have a reasonable belief would be contrary to the user’s wish, or to ask the user before making some change. [Authenticator actions](#signal-method-authenticator-actions) are thus provided as the recommended way to handle [signal methods](#signal-methods).

In cases where an [authenticator](#authenticator) does not have the capability to process an [authenticator action](#signal-method-authenticator-actions), [clients](#client) MAY choose to use existing infrastructure such as [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") ’s `authenticatorCredentialManagement` command to achieve an equivalent effect.

Note: [Signal methods](#signal-methods) intentionally avoid waiting for [authenticators](#authenticator) to complete executing the [authenticator actions](#signal-method-authenticator-actions). This measure protects users from [WebAuthn Relying Parties](#webauthn-relying-party) gaining information about availability of their credentials without based on the timing of the request.

##### 5.1.10.1. Asynchronous RP ID validation algorithm

The [Asynchronous RP ID validation algorithm](#abstract-opdef-asynchronous-rp-id-validation-algorithm) lets [signal methods](#signal-methods) validate [RP IDs](#rp-id) [in parallel](https://html.spec.whatwg.org/multipage/infrastructure.html#in-parallel). The algorithm takes a `DOMString` rpId as input and returns a promise that rejects if the validation fails. The steps are:

1. Let effectiveDomain be the ’s [origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-origin) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain). If effectiveDomain is not a [valid domain](https://url.spec.whatwg.org/#valid-domain), then return [a promise rejected with](https://webidl.spec.whatwg.org/#a-promise-rejected-with) " `SecurityError` " `DOMException`.
2. If rpId [is a registrable domain suffix of or is equal to](https://html.spec.whatwg.org/multipage/browsers.html#is-a-registrable-domain-suffix-of-or-is-equal-to) effectiveDomain, return [a promise resolved with](https://webidl.spec.whatwg.org/#a-promise-resolved-with) undefined.
3. If the client does not support [related origin requests](#sctn-related-origins), return [a promise rejected with](https://webidl.spec.whatwg.org/#a-promise-rejected-with) a " `SecurityError` " `DOMException`.
4. Let p be [a new promise](https://webidl.spec.whatwg.org/#a-new-promise).
5. Execute the following steps [in parallel](https://html.spec.whatwg.org/multipage/infrastructure.html#in-parallel):
	1. If the result of running the with arguments callerOrigin and rpId is `true`, then [resolve](https://webidl.spec.whatwg.org/#resolve) p.
		2. Otherwise, [reject](https://webidl.spec.whatwg.org/#reject) p with a " `SecurityError` " `DOMException`.
6. Return p.

##### 5.1.10.2.

The `signalUnknownCredential` method signals that a [credential id](#credential-id) was not recognized by the [WebAuthn Relying Party](#webauthn-relying-party), e.g. because it was deleted by the user. Unlike `signalAllAcceptedCredentials(options)`, this method does not require passing the entire list of accepted [credential IDs](#credential-id) and the [userHandle](#public-key-credential-source-userhandle), avoiding a privacy leak to an unauthenticated caller (see [§ 14.6.3 Privacy leak via credential IDs](#sctn-credential-id-privacy-leak)).

Upon invocation of `signalUnknownCredential(options)`, the [client](#client) executes these steps:

1. If the result of [base64url decoding](#base64url-encoding) `` options.`credentialId` `` is an error, then return [a promise rejected with](https://webidl.spec.whatwg.org/#a-promise-rejected-with) a `TypeError`.
2. Let p be the result of executing the [Asynchronous RP ID validation algorithm](#abstract-opdef-asynchronous-rp-id-validation-algorithm) with `` options.`rpId` ``.
3. [Upon fulfillment](https://webidl.spec.whatwg.org/#upon-fulfillment) of p, run the following steps [in parallel](https://html.spec.whatwg.org/multipage/infrastructure.html#in-parallel):
	1. For every [authenticator](#authenticator) presently available on this [client platform](#client-platform), invoke the [unknownCredentialId](#signal-method-authenticator-action-unknowncredentialid) [authenticator action](#signal-method-authenticator-actions) with options as input.
4. Return p.

The unknownCredentialId [authenticator action](#signal-method-authenticator-actions) takes an `UnknownCredentialOptions` options and is as follows:

1. [For each](https://infra.spec.whatwg.org/#map-iterate) [public key credential source](#public-key-credential-source) credential in the [authenticator](#authenticator) ’s [credential map](#authenticator-credentials-map):
	1. If the credential ’s [rpId](#public-key-credential-source-rpid) equals `` options.`rpId` `` and the credential ’s [id](#public-key-credential-source-id) equals the result of [base64url decoding](#base64url-encoding) `` options.`credentialId` ``, [remove](https://infra.spec.whatwg.org/#map-remove) credential from the [credentials map](#authenticator-credentials-map) or employ an [authenticator](#authenticator) -specific procedure to hide it from future [authentication ceremonies](#authentication-ceremony).

A user deletes a [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) on a [WebAuthn Relying Party](#webauthn-relying-party) provided UI. Later, when trying to authenticate to the [WebAuthn Relying Party](#webauthn-relying-party) with an [empty](https://infra.spec.whatwg.org/#list-empty) `allowCredentials`, the [authenticator](#authenticator) UI offers them the [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) they previously deleted. The user selects that [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential). After rejecting the sign-in attempt, the [WebAuthn Relying Party](#webauthn-relying-party) runs:

```javascript
PublicKeyCredential.signalUnknownCredential({
    rpId: "example.com",
    credentialId: "aabbcc"  // credential id the user just tried, base64url
});
```

The [authenticator](#authenticator) then deletes or hides the [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) from future [authentication ceremonies](#authentication-ceremony).

##### 5.1.10.3.

Signals the complete list of [credential ids](#credential-id) for a given user. [WebAuthn Relying Parties](#webauthn-relying-party) SHOULD prefer this method over `signalUnknownCredential()` when the user is authenticated and therefore there is no privacy leak risk (see [§ 14.6.3 Privacy leak via credential IDs](#sctn-credential-id-privacy-leak)), since the list offers a full snapshot of a user’s [public key credentials](#public-key-credential) and might reflect changes that haven’t yet been reported to currently attached authenticators.

Upon invocation of `signalAllAcceptedCredentials(options)`, the [client](#client) executes these steps:

1. If the result of [base64url decoding](#base64url-encoding) `` options.`userId` `` is an error, then return [a promise rejected with](https://webidl.spec.whatwg.org/#a-promise-rejected-with) a `TypeError`.
2. [For each](https://infra.spec.whatwg.org/#list-iterate) credentialId in `` options.`allAcceptedCredentialIds` ``:
	1. If the result of [base64url decoding](#base64url-encoding) credentialId is an error, then return [a promise rejected with](https://webidl.spec.whatwg.org/#a-promise-rejected-with) a `TypeError`.
3. Let p be the result of executing the [Asynchronous RP ID validation algorithm](#abstract-opdef-asynchronous-rp-id-validation-algorithm) with `` options.`rpId` ``.
4. [Upon fulfillment](https://webidl.spec.whatwg.org/#upon-fulfillment) of p, run the following steps [in parallel](https://html.spec.whatwg.org/multipage/infrastructure.html#in-parallel):
	1. For every [authenticator](#authenticator) presently available on this [client platform](#client-platform), invoke the [allAcceptedCredentialIds](#signal-method-authenticator-actions-allacceptedcredentialids) [authenticator action](#signal-method-authenticator-actions) with options as input.
5. Return p.

The allAcceptedCredentialIds [authenticator actions](#signal-method-authenticator-actions) take an `AllAcceptedCredentialsOptions` options and are as follows:

1. Let userId be result of [base64url decoding](#base64url-encoding) `` options.`userId` ``.
2. Assertion: userId is not an error.
3. Let credential be ``credentials map[options.`rpId`, userId]``.
4. If credential does not exist, abort these steps.
5. If `` options.`allAcceptedCredentialIds` `` does NOT [contain](https://infra.spec.whatwg.org/#list-contain) the result of [base64url encoding](#base64url-encoding) the credential ’s [id](#public-key-credential-source-id), then [remove](https://infra.spec.whatwg.org/#map-remove) credential from the [credentials map](#authenticator-credentials-map) or employ an [authenticator](#authenticator) -specific procedure to hide it from future [authentication ceremonies](#authentication-ceremony).
6. Else, if credential has been hidden by an [authenticator](#authenticator) -specific procecure, reverse the action so that credential is present in future [authentication ceremonies](#authentication-ceremony).

A user has two credentials with [credential ids](#credential-id) that [base64url encode](#base64url-encoding) to `aa` and `bb`. The user deletes the credential `aa` on a [WebAuthn Relying Party](#webauthn-relying-party) provided UI. The [WebAuthn Relying Party](#webauthn-relying-party) runs:

```javascript
PublicKeyCredential.signalAllAcceptedCredentials({
    rpId: "example.com",
    userId: "aabbcc",  // user handle, base64url.
    allAcceptedCredentialIds: [
        "bb",
    ]
});
```

If the [authenticator](#authenticator) is attached at the time of execution, it deletes or hides the [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) corresponding to `aa` from future [authentication ceremonies](#authentication-ceremony).

Note: [Authenticators](#authenticator) might not be attached at the time `signalAllAcceptedCredentials(options)` is executed. Therefore, [WebAuthn Relying Parties](#webauthn-relying-party) may choose to run `signalAllAcceptedCredentials(options)` periodically, e.g. on every sign in.

Note: Credentials not present in `allAcceptedCredentialIds` will be removed or hidden, potentially irreversibly. [Relying parties](#relying-party) must exercise care that valid credential IDs are never omitted from the list. If a valid [credential ID](#credential-id) were accidentally omitted, the [relying party](#relying-party) should immediately include it in `signalAllAcceptedCredentials(options)` as soon as possible to "unhide" it, if supported by the [authenticator](#authenticator).

[Authenticators](#authenticator) SHOULD, whenever possible, prefer hiding [public key credentials](#public-key-credential) for a period of time instead of permanently removing them, to aid recovery if a [WebAuthn Relying Party](#webauthn-relying-party) accidentally omits valid [credential IDs](#credential-id) from `allAcceptedCredentialIds`.

##### 5.1.10.4.

The `signalCurrentUserDetails` method signals the user’s current `name` and `displayName`.

Upon invocation of `signalCurrentUserDetails(options)`, the [client](#client) executes these steps:

1. If the result of [base64url decoding](#base64url-encoding) `` options.`userId` `` is an error, then return [a promise rejected with](https://webidl.spec.whatwg.org/#a-promise-rejected-with) a `TypeError`.
2. Let p be the result of executing the [Asynchronous RP ID validation algorithm](#abstract-opdef-asynchronous-rp-id-validation-algorithm) with `` options.`rpId` ``.
3. [Upon fulfillment](https://webidl.spec.whatwg.org/#upon-fulfillment) of p, run the following steps [in parallel](https://html.spec.whatwg.org/multipage/infrastructure.html#in-parallel):
	1. For every [authenticator](#authenticator) presently available on this [client platform](#client-platform), invoke the [currentUserDetails](#signal-method-authenticator-actions-currentuserdetails) [authenticator action](#signal-method-authenticator-actions) with options as input.
4. Return p.

The currentUserDetails [authenticator action](#signal-method-authenticator-actions) takes a `CurrentUserDetailsOptions` options and is as follows:

1. Let userId be result of [base64url decoding](#base64url-encoding) `` options.`userId` ``.
2. Assertion: userId is not an error.
3. Let credential be ``credentials map[options.`rpId`, userId]``.
4. If credential does not exist, abort these steps.
5. Update the credential ’s [otherUI](#public-key-credential-source-otherui) to match `` options.`name` `` and `` options.`displayName` ``.

A user updates their name on a [WebAuthn Relying Party](#webauthn-relying-party) provided UI. The [WebAuthn Relying Party](#webauthn-relying-party) runs:

```javascript
PublicKeyCredential.signalCurrentUserDetails({
    rpId: "example.com",
    userId: "aabbcc",  // user handle, base64url.
    name: "New user name",
    displayName: "New display name",
});
```

The [authenticator](#authenticator) then updates the [otherUI](#public-key-credential-source-otherui) of the matching credential.

Note: [Authenticators](#authenticator) might not be attached at the time `signalCurrentUserDetails(options)` is executed. Therefore, [WebAuthn Relying Parties](#webauthn-relying-party) may choose to run `signalCurrentUserDetails(options)` periodically, e.g. on every sign in.

### 5.2. Authenticator Responses (interface AuthenticatorResponse)

[Authenticators](#authenticator) respond to [Relying Party](#relying-party) requests by returning an object derived from the `AuthenticatorResponse` interface:

```
[SecureContext, Exposed=Window]
interface AuthenticatorResponse {
    [SameObject] readonly attribute ArrayBuffer      clientDataJSON;
};
```

`clientDataJSON`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer), readonly

This attribute contains a [JSON-compatible serialization](#clientdatajson-serialization) of the [client data](#client-data), the [hash of which](#collectedclientdata-hash-of-the-serialized-client-data) is passed to the authenticator by the client in its call to either `create()` or `get()` (i.e., the [client data](#client-data) itself is not sent to the authenticator).

#### 5.2.1. Information About Public Key Credential (interface AuthenticatorAttestationResponse)

The `AuthenticatorAttestationResponse` interface represents the [authenticator](#authenticator) ’s response to a client’s request for the creation of a new [public key credential](#public-key-credential). It contains information about the new credential that can be used to identify it for later use, and metadata that can be used by the [WebAuthn Relying Party](#webauthn-relying-party) to assess the characteristics of the credential during registration.

```
[SecureContext, Exposed=Window]
interface AuthenticatorAttestationResponse : AuthenticatorResponse {
    [SameObject] readonly attribute ArrayBuffer      attestationObject;
    sequence<DOMString>                              getTransports();
    ArrayBuffer                                      getAuthenticatorData();
    ArrayBuffer?                                     getPublicKey();
    COSEAlgorithmIdentifier                          getPublicKeyAlgorithm();
};
```

`clientDataJSON`

This attribute, inherited from `AuthenticatorResponse`, contains the [JSON-compatible serialization of client data](#collectedclientdata-json-compatible-serialization-of-client-data) (see [§ 6.5 Attestation](#sctn-attestation)) passed to the authenticator by the client in order to generate this credential. The exact JSON serialization MUST be preserved, as the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data) has been computed over it.

`attestationObject`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer), readonly

This attribute contains an [attestation object](#attestation-object), which is opaque to, and cryptographically protected against tampering by, the client. The [attestation object](#attestation-object) contains both [authenticator data](#authenticator-data) and an [attestation statement](#attestation-statement). The former contains the AAGUID, a unique [credential ID](#credential-id), and the [credential public key](#credential-public-key). The contents of the [attestation statement](#attestation-statement) are determined by the [attestation statement format](#attestation-statement-format) used by the [authenticator](#authenticator). It also contains any additional information that the [Relying Party](#relying-party) ’s server requires to validate the [attestation statement](#attestation-statement), as well as to decode and validate the [authenticator data](#authenticator-data) along with the [JSON-compatible serialization of client data](#collectedclientdata-json-compatible-serialization-of-client-data). For more details, see [§ 6.5 Attestation](#sctn-attestation), [§ 6.5.4 Generating an Attestation Object](#sctn-generating-an-attestation-object), and [Figure 6](#fig-attStructs).

`getTransports()`

This operation returns the value of `[[transports]]`.

`getAuthenticatorData()`

This operation returns the [authenticator data](#authenticator-data) contained within `attestationObject`. See [§ 5.2.1.1 Easily accessing credential data](#sctn-public-key-easy).

`getPublicKey()`

This operation returns the DER [SubjectPublicKeyInfo](https://tools.ietf.org/html/rfc5280#section-4.1.2.7) of the new credential, or null if this is not available. See [§ 5.2.1.1 Easily accessing credential data](#sctn-public-key-easy).

`getPublicKeyAlgorithm()`

This operation returns the `COSEAlgorithmIdentifier` of the new credential. See [§ 5.2.1.1 Easily accessing credential data](#sctn-public-key-easy).

`[[transports]]`

This [internal slot](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) contains a sequence of zero or more unique `DOMString` s in lexicographical order. These values are the transports that the [authenticator](#authenticator) is believed to support, or an empty sequence if the information is unavailable. The values SHOULD be members of `AuthenticatorTransport` but [Relying Parties](#relying-party) SHOULD accept and store unknown values.

##### 5.2.1.1. Easily accessing credential data

Every user of the `[[Create]](origin, options, sameOriginWithAncestors)` method will need to parse and store the returned [credential public key](#credential-public-key) in order to verify future [authentication assertions](#authentication-assertion). However, the [credential public key](#credential-public-key) is in COSE format [\[RFC9052\]](#biblio-rfc9052 "CBOR Object Signing and Encryption (COSE): Structures and Process"), inside the [credentialPublicKey](#authdata-attestedcredentialdata-credentialpublickey) member of the [attestedCredentialData](#authdata-attestedcredentialdata), inside the [authenticator data](#authenticator-data), inside the [attestation object](#attestation-object) conveyed by `AuthenticatorAttestationResponse`.`attestationObject`. [Relying Parties](#relying-party) wishing to use [attestation](#attestation) are obliged to do the work of parsing the `attestationObject` and obtaining the [credential public key](#credential-public-key) because that public key copy is the one the [authenticator](#authenticator) [signed](#signing-procedure). However, many valid WebAuthn use cases do not require [attestation](#attestation). For those uses, user agents can do the work of parsing, expose the [authenticator data](#authenticator-data) directly, and translate the [credential public key](#credential-public-key) into a more convenient format.

The `getPublicKey()` operation thus returns the [credential public key](#credential-public-key) as a [SubjectPublicKeyInfo](https://tools.ietf.org/html/rfc5280#section-4.1.2.7). This `ArrayBuffer` can, for example, be passed to Java’s `java.security.spec.X509EncodedKeySpec`,.NET’s `System.Security.Cryptography.ECDsa.ImportSubjectPublicKeyInfo`, or Go’s `crypto/x509.ParsePKIXPublicKey`.

Use of `getPublicKey()` does impose some limitations: by using `pubKeyCredParams`, a [Relying Party](#relying-party) can negotiate with the [authenticator](#authenticator) to use public key algorithms that the user agent may not understand. However, if the [Relying Party](#relying-party) does so, the user agent will not be able to translate the resulting [credential public key](#credential-public-key) into [SubjectPublicKeyInfo](https://tools.ietf.org/html/rfc5280#section-4.1.2.7) format and the return value of `getPublicKey()` will be null.

User agents MUST be able to return a non-null value for `getPublicKey()` when the [credential public key](#credential-public-key) has a `COSEAlgorithmIdentifier` value of:

- \-7 (ES256), where [kty](https://tools.ietf.org/html/rfc9052#name-cose-key-common-parameters) is 2 (with uncompressed points) and [crv](https://tools.ietf.org/html/rfc9053#name-double-coordinate-curves) is 1 (P-256).
- \-257 (RS256).
- \-8 (EdDSA), where [crv](https://tools.ietf.org/html/rfc9053#name-double-coordinate-curves) is 6 (Ed25519).

A [SubjectPublicKeyInfo](https://tools.ietf.org/html/rfc5280#section-4.1.2.7) does not include information about the signing algorithm (for example, which hash function to use) that is included in the COSE public key. To provide this, `getPublicKeyAlgorithm()` returns the `COSEAlgorithmIdentifier` for the [credential public key](#credential-public-key).

To remove the need to parse CBOR at all in many cases, `getAuthenticatorData()` returns the [authenticator data](#authenticator-data) from `attestationObject`. The [authenticator data](#authenticator-data) contains other fields that are encoded in a binary format. However, helper functions are not provided to access them because [Relying Parties](#relying-party) already need to extract those fields when [getting an assertion](#sctn-getAssertion). In contrast to [credential creation](#sctn-createCredential), where signature verification is [optional](#enumdef-attestationconveyancepreference), [Relying Parties](#relying-party) should always be verifying signatures from an assertion and thus must extract fields from the signed [authenticator data](#authenticator-data). The same functions used there will also serve during credential creation.

[Relying Parties](#relying-party) SHOULD use feature detection before using these functions by testing the value of `'getPublicKey' in AuthenticatorAttestationResponse.prototype`.

Note: `getPublicKey()` and `getAuthenticatorData()` were only added in Level 2 of this specification. [Relying Parties](#relying-party) that require these functions to exist may not interoperate with older user-agents.

#### 5.2.2. Web Authentication Assertion (interface AuthenticatorAssertionResponse)

The `AuthenticatorAssertionResponse` interface represents an [authenticator](#authenticator) ’s response to a client’s request for generation of a new [authentication assertion](#authentication-assertion) given the [WebAuthn Relying Party](#webauthn-relying-party) ’s challenge and OPTIONAL list of credentials it is aware of. This response contains a cryptographic signature proving possession of the [credential private key](#credential-private-key), and optionally evidence of to a specific transaction.

```
[SecureContext, Exposed=Window]
interface AuthenticatorAssertionResponse : AuthenticatorResponse {
    [SameObject] readonly attribute ArrayBuffer      authenticatorData;
    [SameObject] readonly attribute ArrayBuffer      signature;
    [SameObject] readonly attribute ArrayBuffer?     userHandle;
};
```

`clientDataJSON`

This attribute, inherited from `AuthenticatorResponse`, contains the [JSON-compatible serialization of client data](#collectedclientdata-json-compatible-serialization-of-client-data) (see [§ 5.8.1 Client Data Used in WebAuthn Signatures (dictionary CollectedClientData)](#dictionary-client-data)) passed to the authenticator by the client in order to generate this assertion. The exact JSON serialization MUST be preserved, as the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data) has been computed over it.

`authenticatorData`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer), readonly

This attribute contains the [authenticator data](#authenticator-data) returned by the authenticator. See [§ 6.1 Authenticator Data](#sctn-authenticator-data).

`signature`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer), readonly

This attribute contains the raw signature returned from the authenticator. See [§ 6.3.3 The authenticatorGetAssertion Operation](#sctn-op-get-assertion).

`userHandle`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer), readonly, nullable

This attribute contains the [user handle](#user-handle) returned from the authenticator, or null if the authenticator did not return a [user handle](#user-handle). See [§ 6.3.3 The authenticatorGetAssertion Operation](#sctn-op-get-assertion). The authenticator MUST always return a [user handle](#user-handle) if the `allowCredentials` option used in the [authentication ceremony](#authentication-ceremony) is [empty](https://infra.spec.whatwg.org/#list-is-empty), and MAY return one otherwise.

### 5.3. Parameters for Credential Generation (dictionary PublicKeyCredentialParameters)

```
dictionary PublicKeyCredentialParameters {
    required DOMString                    type;
    required COSEAlgorithmIdentifier      alg;
};
```

This dictionary is used to supply additional parameters when creating a new credential.

`type`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

This member specifies the type of credential to be created. The value SHOULD be a member of `PublicKeyCredentialType` but [client platforms](#client-platform) MUST ignore unknown values, ignoring any `PublicKeyCredentialParameters` with an unknown `type`.

`alg`, of type [COSEAlgorithmIdentifier](#typedefdef-cosealgorithmidentifier)

This member specifies the cryptographic signature algorithm with which the newly generated credential will be used, and thus also the type of asymmetric key pair to be generated, e.g., RSA or Elliptic Curve.

Note: we use "alg" as the latter member name, rather than spelling-out "algorithm", because it will be serialized into a message to the authenticator, which may be sent over a low-bandwidth link.

### 5.4. Options for Credential Creation (dictionary PublicKeyCredentialCreationOptions)

```
dictionary PublicKeyCredentialCreationOptions {
    required PublicKeyCredentialRpEntity         rp;
    required PublicKeyCredentialUserEntity       user;

    required BufferSource                             challenge;
    required sequence<PublicKeyCredentialParameters>  pubKeyCredParams;

    unsigned long                                timeout;
    sequence<PublicKeyCredentialDescriptor>      excludeCredentials = [];
    AuthenticatorSelectionCriteria               authenticatorSelection;
    sequence<DOMString>                          hints = [];
    DOMString                                    attestation = "none";
    sequence<DOMString>                          attestationFormats = [];
    AuthenticationExtensionsClientInputs         extensions;
};
```

`rp`, of type [PublicKeyCredentialRpEntity](#dictdef-publickeycredentialrpentity)

This member contains a name and an identifier for the [Relying Party](#relying-party) responsible for the request.

Its value’s `name` member is REQUIRED. See [§ 5.4.1 Public Key Entity Description (dictionary PublicKeyCredentialEntity)](#dictionary-pkcredentialentity) for further details.

Its value’s `id` member specifies the [RP ID](#rp-id) the credential should be [scoped](#scope) to. If omitted, its value will be the `CredentialsContainer` object’s ’s [origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-origin) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain). See [§ 5.4.2 Relying Party Parameters for Credential Generation (dictionary PublicKeyCredentialRpEntity)](#dictionary-rp-credential-params) for further details.

`user`, of type [PublicKeyCredentialUserEntity](#dictdef-publickeycredentialuserentity)

This member contains names and an identifier for the [user account](#user-account) performing the [registration](#registration).

Its value’s `name`, `displayName` and `id` members are REQUIRED. `id` can be returned as the `userHandle` in some future [authentication ceremonies](#authentication-ceremony), and is used to overwrite existing [discoverable credentials](#discoverable-credential) that have the same `` `rp`.`id` `` and `` `user`.`id` `` on the same [authenticator](#authenticator). `name` and `displayName` MAY be used by the [authenticator](#authenticator) and [client](#client) in future [authentication ceremonies](#authentication-ceremony) to help the user select a [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential), but are not returned to the [Relying Party](#relying-party) as a result of future [authentication ceremonies](#authentication-ceremony)

For further details, see [§ 5.4.1 Public Key Entity Description (dictionary PublicKeyCredentialEntity)](#dictionary-pkcredentialentity) and [§ 5.4.3 User Account Parameters for Credential Generation (dictionary PublicKeyCredentialUserEntity)](#dictionary-user-credential-params).

`challenge`, of type [BufferSource](https://webidl.spec.whatwg.org/#BufferSource)

This member specifies a challenge that the [authenticator](#authenticator) signs, along with other data, when producing an [attestation object](#attestation-object) for the newly created credential. See the [§ 13.4.3 Cryptographic Challenges](#sctn-cryptographic-challenges) security consideration.

`pubKeyCredParams`, of type sequence< [PublicKeyCredentialParameters](#dictdef-publickeycredentialparameters) >

This member lists the key types and signature algorithms the [Relying Party](#relying-party) supports, ordered from most preferred to least preferred. Duplicates are allowed but effectively ignored. The [client](#client) and [authenticator](#authenticator) make a best-effort to create a credential of the most preferred type possible. If none of the listed types can be created, the `create()` operation fails.

[Relying Parties](#relying-party) that wish to support a wide range of [authenticators](#authenticator) SHOULD include at least the following `COSEAlgorithmIdentifier` values:

- \-8 (EdDSA)
- \-7 (ES256)
- \-257 (RS256)

Additional signature algorithms can be included as needed.

The following `COSEAlgorithmIdentifier` values are NOT RECOMMENDED in `pubKeyCredParams`:

- \-9 (ESP256); use -7 (ES256) instead or in addition.
- \-51 (ESP384); use -35 (ES384) instead or in addition.
- \-52 (ESP512); use -36 (ES512) instead or in addition.
- \-19 (Ed25519); use -8 (EdDSA) instead or in addition.

Note: Within WebAuthn, the values -9 (ESP256), -51 (ESP384), -52 (ESP512) and -19 (Ed25519) represent the same thing respectively as -7 (ES256), -35 (ES384), -36 (ES512) and -8 (EdDSA) because of the additional restrictions stated in [§ 5.8.5 Cryptographic Algorithm Identifier (typedef COSEAlgorithmIdentifier)](#sctn-alg-identifier). However, they are not interchangeable in practice since many implementations support the latter identifiers but not the former ones. Therefore the latter identifiers are preferred in `pubKeyCredParams` for backwards compatibility.

`timeout`, of type [unsigned long](https://webidl.spec.whatwg.org/#idl-unsigned-long)

This OPTIONAL member specifies a time, in milliseconds, that the [Relying Party](#relying-party) is willing to wait for the call to complete. This is treated as a hint, and MAY be overridden by the [client](#client).

`excludeCredentials`, of type sequence< [PublicKeyCredentialDescriptor](#dictdef-publickeycredentialdescriptor) >, defaulting to `[]`

The [Relying Party](#relying-party) SHOULD use this OPTIONAL member to list any existing [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential) mapped to this [user account](#user-account) (as identified by `user`.`id`). This ensures that the new credential is not [created on](#created-on) an [authenticator](#authenticator) that already [contains](#contains) a credential mapped to this [user account](#user-account). If it would be, the [client](#client) is requested to instead guide the user to use a different [authenticator](#authenticator), or return an error if that fails.

`authenticatorSelection`, of type [AuthenticatorSelectionCriteria](#dictdef-authenticatorselectioncriteria)

The [Relying Party](#relying-party) MAY use this OPTIONAL member to specify capabilities and settings that the [authenticator](#authenticator) MUST or SHOULD satisfy to participate in the `create()` operation. See [§ 5.4.4 Authenticator Selection Criteria (dictionary AuthenticatorSelectionCriteria)](#dictionary-authenticatorSelection).

`hints`, of type sequence< [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString) >, defaulting to `[]`

This OPTIONAL member contains zero or more elements from `PublicKeyCredentialHint` to guide the user agent in interacting with the user. Note that the elements have type `DOMString` despite being taken from that enumeration. See [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

`attestation`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString), defaulting to `"none"`

The [Relying Party](#relying-party) MAY use this OPTIONAL member to specify a preference regarding [attestation conveyance](#attestation-conveyance). Its value SHOULD be a member of `AttestationConveyancePreference`. [Client platforms](#client-platform) MUST ignore unknown values, treating an unknown value as if the [member does not exist](https://infra.spec.whatwg.org/#map-exists).

The default value is `none`.

`attestationFormats`, of type sequence< [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString) >, defaulting to `[]`

The [Relying Party](#relying-party) MAY use this OPTIONAL member to specify a preference regarding the [attestation](#attestation) statement format used by the [authenticator](#authenticator). Values SHOULD be taken from the IANA "WebAuthn Attestation Statement Format Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)"). Values are ordered from most preferred to least preferred. Duplicates are allowed but effectively ignored. This parameter is advisory and the [authenticator](#authenticator) MAY use an attestation statement not enumerated in this parameter.

The default value is the empty list, which indicates no preference.

`extensions`, of type [AuthenticationExtensionsClientInputs](#dictdef-authenticationextensionsclientinputs)

The [Relying Party](#relying-party) MAY use this OPTIONAL member to provide [client extension inputs](#client-extension-input) requesting additional processing by the [client](#client) and [authenticator](#authenticator). For example, the [Relying Party](#relying-party) may request that the client returns additional information about the [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) that was created.

The extensions framework is defined in [§ 9 WebAuthn Extensions](#sctn-extensions). Some extensions are defined in [§ 10 Defined Extensions](#sctn-defined-extensions); consult the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)") for an up-to-date list of registered [WebAuthn Extensions](#webauthn-extensions).

#### 5.4.1. Public Key Entity Description (dictionary PublicKeyCredentialEntity)

The `PublicKeyCredentialEntity` dictionary describes a [user account](#user-account), or a [WebAuthn Relying Party](#webauthn-relying-party), which a [public key credential](#public-key-credential) is associated with or [scoped](#scope) to, respectively.

```
dictionary PublicKeyCredentialEntity {
    required DOMString    name;
};
```

`name`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

A [human-palatable](#human-palatability) name for the entity. Its function depends on what the `PublicKeyCredentialEntity` represents:

- \[DEPRECATED\] When inherited by `PublicKeyCredentialRpEntity` it is a [human-palatable](#human-palatability) identifier for the [Relying Party](#relying-party), intended only for display. For example, "ACME Corporation", "Wonderful Widgets, Inc." or "ОАО Примертех".
	This member is deprecated because many [clients](#client) do not display it, but it remains a required dictionary member for backwards compatibility. [Relying Parties](#relying-party) MAY, as a safe default, set this equal to the [RP ID](#rp-id).
	- [Relying Parties](#relying-party) SHOULD perform enforcement, as prescribed in Section 2.3 of [\[RFC8266\]](#biblio-rfc8266 "Preparation, Enforcement, and Comparison of Internationalized Strings Representing Nicknames") for the Nickname Profile of the PRECIS FreeformClass [\[RFC8264\]](#biblio-rfc8264 "PRECIS Framework: Preparation, Enforcement, and Comparison of Internationalized Strings in Application Protocols"), when setting `name` ’s value, or displaying the value to the user.
		- [Clients](#client) SHOULD perform enforcement, as prescribed in Section 2.3 of [\[RFC8266\]](#biblio-rfc8266 "Preparation, Enforcement, and Comparison of Internationalized Strings Representing Nicknames") for the Nickname Profile of the PRECIS FreeformClass [\[RFC8264\]](#biblio-rfc8264 "PRECIS Framework: Preparation, Enforcement, and Comparison of Internationalized Strings in Application Protocols"), on `name` ’s value prior to displaying the value to the user or including the value as a parameter of the [authenticatorMakeCredential](#authenticatormakecredential) operation.
- When inherited by `PublicKeyCredentialUserEntity`, it is a [human-palatable](#human-palatability) identifier for a [user account](#user-account). This identifier is the primary value displayed to users by [Clients](#client) to help users understand with which [user account](#user-account) a credential is associated.
	Examples of suitable values for this identifier include, "alexm", "+14255551234", "alex.mueller@example.com", "alex.mueller@example.com (prod-env)", or "alex.mueller@example.com (ОАО Примертех)".
	- The [Relying Party](#relying-party) MAY let the user choose this value. The [Relying Party](#relying-party) SHOULD perform enforcement, as prescribed in Section 3.4.3 of [\[RFC8265\]](#biblio-rfc8265 "Preparation, Enforcement, and Comparison of Internationalized Strings Representing Usernames and Passwords") for the UsernameCasePreserved Profile of the PRECIS IdentifierClass [\[RFC8264\]](#biblio-rfc8264 "PRECIS Framework: Preparation, Enforcement, and Comparison of Internationalized Strings in Application Protocols"), when setting `name` ’s value, or displaying the value to the user.
		- [Clients](#client) SHOULD perform enforcement, as prescribed in Section 3.4.3 of [\[RFC8265\]](#biblio-rfc8265 "Preparation, Enforcement, and Comparison of Internationalized Strings Representing Usernames and Passwords") for the UsernameCasePreserved Profile of the PRECIS IdentifierClass [\[RFC8264\]](#biblio-rfc8264 "PRECIS Framework: Preparation, Enforcement, and Comparison of Internationalized Strings in Application Protocols"), on `name` ’s value prior to displaying the value to the user or including the value as a parameter of the [authenticatorMakeCredential](#authenticatormakecredential) operation.

When [clients](#client), [client platforms](#client-platform), or [authenticators](#authenticator) display a `name` ’s value, they should always use UI elements to provide a clear boundary around the displayed value, and not allow overflow into other elements [\[css-overflow-3\]](#biblio-css-overflow-3 "CSS Overflow Module Level 3").

When storing a `name` member’s value, the value MAY be truncated as described in [§ 6.4.1 String Truncation](#sctn-strings-truncation) using a size limit greater than or equal to 64 bytes.

#### 5.4.2. Relying Party Parameters for Credential Generation (dictionary PublicKeyCredentialRpEntity)

The `PublicKeyCredentialRpEntity` dictionary is used to supply additional [Relying Party](#relying-party) attributes when creating a new credential.

```
dictionary PublicKeyCredentialRpEntity : PublicKeyCredentialEntity {
    DOMString      id;
};
```

`id`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

A unique identifier for the [Relying Party](#relying-party) entity, which sets the [RP ID](#rp-id).

#### 5.4.3. User Account Parameters for Credential Generation (dictionary PublicKeyCredentialUserEntity)

The `PublicKeyCredentialUserEntity` dictionary is used to supply additional [user account](#user-account) attributes when creating a new credential.

```
dictionary PublicKeyCredentialUserEntity : PublicKeyCredentialEntity {
    required BufferSource   id;
    required DOMString      displayName;
};
```

`id`, of type [BufferSource](https://webidl.spec.whatwg.org/#BufferSource)

The [user handle](#user-handle) of the [user account](#user-account). A [user handle](#user-handle) is an opaque [byte sequence](https://infra.spec.whatwg.org/#byte-sequence) with a maximum size of 64 bytes, and is not meant to be displayed to the user.

To ensure secure operation, authentication and authorization decisions MUST be made on the basis of this `id` member, not the `displayName` nor `name` members. See Section 6.1 of [\[RFC8266\]](#biblio-rfc8266 "Preparation, Enforcement, and Comparison of Internationalized Strings Representing Nicknames").

The [user handle](#user-handle) MUST NOT contain personally identifying information about the user, such as a username or e-mail address; see [§ 14.6.1 User Handle Contents](#sctn-user-handle-privacy) for details. The [user handle](#user-handle) MUST NOT be empty.

The [user handle](#user-handle) SHOULD NOT be a constant value across different [user accounts](#user-account), even for [non-discoverable credentials](#non-discoverable-credential), because some authenticators always create [discoverable credentials](#discoverable-credential). Thus a constant [user handle](#user-handle) would prevent a user from using such an authenticator with more than one [user account](#user-account) at the [Relying Party](#relying-party).

`displayName`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

A [human-palatable](#human-palatability) name for the [user account](#user-account), intended only for display. The [Relying Party](#relying-party) SHOULD let the user choose this, and SHOULD NOT restrict the choice more than necessary. If no suitable or [human-palatable](#human-palatability) name is available, the [Relying Party](#relying-party) SHOULD set this value to an empty string.

Examples of suitable values for this identifier include, "Alex Müller", "Alex Müller (ACME Co.)" or "田中倫".

- [Relying Parties](#relying-party) SHOULD perform enforcement, as prescribed in Section 2.3 of [\[RFC8266\]](#biblio-rfc8266 "Preparation, Enforcement, and Comparison of Internationalized Strings Representing Nicknames") for the Nickname Profile of the PRECIS FreeformClass [\[RFC8264\]](#biblio-rfc8264 "PRECIS Framework: Preparation, Enforcement, and Comparison of Internationalized Strings in Application Protocols"), when setting `displayName` ’s value to a non-empty string, or displaying a non-empty value to the user.
- [Clients](#client) SHOULD perform enforcement, as prescribed in Section 2.3 of [\[RFC8266\]](#biblio-rfc8266 "Preparation, Enforcement, and Comparison of Internationalized Strings Representing Nicknames") for the Nickname Profile of the PRECIS FreeformClass [\[RFC8264\]](#biblio-rfc8264 "PRECIS Framework: Preparation, Enforcement, and Comparison of Internationalized Strings in Application Protocols"), on `displayName` ’s value prior to displaying a non-empty value to the user or including a non-empty value as a parameter of the [authenticatorMakeCredential](#authenticatormakecredential) operation.

When [clients](#client), [client platforms](#client-platform), or [authenticators](#authenticator) display a `displayName` ’s value, they should always use UI elements to provide a clear boundary around the displayed value, and not allow overflow into other elements [\[css-overflow-3\]](#biblio-css-overflow-3 "CSS Overflow Module Level 3").

When storing a `displayName` member’s value, the value MAY be truncated as described in [§ 6.4.1 String Truncation](#sctn-strings-truncation) using a size limit greater than or equal to 64 bytes.

#### 5.4.4. Authenticator Selection Criteria (dictionary AuthenticatorSelectionCriteria)

[WebAuthn Relying Parties](#webauthn-relying-party) may use the `AuthenticatorSelectionCriteria` dictionary to specify their requirements regarding authenticator attributes.

```
dictionary AuthenticatorSelectionCriteria {
    DOMString                    authenticatorAttachment;
    DOMString                    residentKey;
    boolean                      requireResidentKey = false;
    DOMString                    userVerification = "preferred";
};
```

`authenticatorAttachment`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

If this member is present, eligible [authenticators](#authenticator) are filtered to be only those authenticators attached with the specified [authenticator attachment modality](#enum-attachment) (see also [§ 6.2.1 Authenticator Attachment Modality](#sctn-authenticator-attachment-modality)). If this member is absent, then any attachment modality is acceptable. The value SHOULD be a member of `AuthenticatorAttachment` but [client platforms](#client-platform) MUST ignore unknown values, treating an unknown value as if the [member does not exist](https://infra.spec.whatwg.org/#map-exists).

See also the `authenticatorAttachment` member of `PublicKeyCredential`, which can tell what was used in a successful `create()` or `get()` operation.

`residentKey`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

Specifies the extent to which the [Relying Party](#relying-party) desires to create a [client-side discoverable credential](#client-side-discoverable-credential). For historical reasons the naming retains the deprecated “resident” terminology. The value SHOULD be a member of `ResidentKeyRequirement` but [client platforms](#client-platform) MUST ignore unknown values, treating an unknown value as if the [member does not exist](https://infra.spec.whatwg.org/#map-exists). If no value is given then the effective value is `required` if `requireResidentKey` is `true` or `discouraged` if it is `false` or absent.

See `ResidentKeyRequirement` for the description of `residentKey` ’s values and semantics.

`requireResidentKey`, of type [boolean](https://webidl.spec.whatwg.org/#idl-boolean), defaulting to `false`

This member is retained for backwards compatibility with WebAuthn Level 1 and, for historical reasons, its naming retains the deprecated “resident” terminology for [discoverable credentials](#discoverable-credential). [Relying Parties](#relying-party) SHOULD set it to `true` if, and only if, `residentKey` is set to `required`.

`userVerification`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString), defaulting to `"preferred"`

This member specifies the [Relying Party](#relying-party) ’s requirements regarding [user verification](#user-verification) for the `create()` operation. The value SHOULD be a member of `UserVerificationRequirement` but [client platforms](#client-platform) MUST ignore unknown values, treating an unknown value as if the [member does not exist](https://infra.spec.whatwg.org/#map-exists).

See `UserVerificationRequirement` for the description of `userVerification` ’s values and semantics.

#### 5.4.5. Authenticator Attachment Enumeration (enum AuthenticatorAttachment)

This enumeration’s values describe [authenticators](#authenticator) '. [Relying Parties](#relying-party) use this to express a preferred when calling `navigator.credentials.create()` to [create a credential](#sctn-createCredential), and [clients](#client) use this to report the used to complete a [registration](#registration-ceremony) or [authentication ceremony](#authentication-ceremony).

```
enum AuthenticatorAttachment {
    "platform",
    "cross-platform"
};
```

Note: The `AuthenticatorAttachment` enumeration is deliberately not referenced, see [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

`platform`

This value indicates [platform attachment](#platform-attachment).

`cross-platform`

This value indicates [cross-platform attachment](#cross-platform-attachment).

Note: An selection option is available only in the `[[Create]](origin, options, sameOriginWithAncestors)` operation. The [Relying Party](#relying-party) may use it to, for example, ensure the user has a [roaming credential](#roaming-credential) for authenticating on another [client device](#client-device); or to specifically register a [platform credential](#platform-credential) for easier reauthentication using a particular [client device](#client-device). The `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` operation has no selection option. The [client](#client) and user will use whichever [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) is available and convenient at the time, subject to the `allowCredentials` option.

#### 5.4.6. Resident Key Requirement Enumeration (enum ResidentKeyRequirement)

```
enum ResidentKeyRequirement {
    "discouraged",
    "preferred",
    "required"
};
```

Note: The `ResidentKeyRequirement` enumeration is deliberately not referenced, see [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

This enumeration’s values describe the [Relying Party](#relying-party) ’s requirements for [client-side discoverable credentials](#client-side-discoverable-credential) (formerly known as [resident credentials](#resident-credential) or [resident keys](#resident-key)):

`discouraged`

The [Relying Party](#relying-party) prefers creating a [server-side credential](#server-side-credential), but will accept a [client-side discoverable credential](#client-side-discoverable-credential). The [client](#client) and [authenticator](#authenticator) SHOULD create a [server-side credential](#server-side-credential) if possible.

Note: A [Relying Party](#relying-party) cannot require that a created credential is a [server-side credential](#server-side-credential) and the [Credential Properties Extension](#credprops) may not return a value for the `rk` property. Because of this, it may be the case that it does not know if a credential is a [server-side credential](#server-side-credential) or not and thus does not know whether creating a second credential with the same [user handle](#user-handle) will evict the first.

`preferred`

The [Relying Party](#relying-party) strongly prefers creating a [client-side discoverable credential](#client-side-discoverable-credential), but will accept a [server-side credential](#server-side-credential). The [client](#client) and [authenticator](#authenticator) SHOULD create a [discoverable credential](#discoverable-credential) if possible. For example, the [client](#client) SHOULD guide the user through setting up [user verification](#user-verification) if needed to create a [discoverable credential](#discoverable-credential). This takes precedence over the setting of `userVerification`.

`required`

The [Relying Party](#relying-party) requires a [client-side discoverable credential](#client-side-discoverable-credential). The [client](#client) MUST return an error if a [client-side discoverable credential](#client-side-discoverable-credential) cannot be created.

Note: The [Relying Party](#relying-party) can seek information on whether or not the authenticator created a [client-side discoverable credential](#client-side-discoverable-credential) using the [resident key credential property](#credentialpropertiesoutput-resident-key-credential-property) of the [Credential Properties Extension](#credprops). This is useful when values of `discouraged` or `preferred` are used for `` options.`authenticatorSelection`.`residentKey` ``, because in those cases it is possible for an [authenticator](#authenticator) to create *either* a [client-side discoverable credential](#client-side-discoverable-credential) or a [server-side credential](#server-side-credential).

#### 5.4.7. Attestation Conveyance Preference Enumeration (enum AttestationConveyancePreference)

[WebAuthn Relying Parties](#webauthn-relying-party) may use `AttestationConveyancePreference` to specify their preference regarding [attestation conveyance](#attestation-conveyance) during credential generation.

```
enum AttestationConveyancePreference {
    "none",
    "indirect",
    "direct",
    "enterprise"
};
```

Note: The `AttestationConveyancePreference` enumeration is deliberately not referenced, see [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

`none`

The [Relying Party](#relying-party) is not interested in [authenticator](#authenticator) [attestation](#attestation). For example, in order to potentially avoid having to obtain to relay identifying information to the [Relying Party](#relying-party), or to save a roundtrip to an [Attestation CA](#attestation-ca) or [Anonymization CA](#anonymization-ca). If the [authenticator](#authenticator) generates an [attestation statement](#attestation-statement) that is not a [self attestation](#self-attestation), the [client](#client) will replace it with a [None](#none) attestation statement.

This is the default, and unknown values fall back to the behavior of this value.

`indirect`

The [Relying Party](#relying-party) wants to receive a verifiable [attestation statement](#attestation-statement), but allows the [client](#client) to decide how to obtain such an [attestation statement](#attestation-statement). The client MAY replace an authenticator-generated [attestation statement](#attestation-statement) with one generated by an [Anonymization CA](#anonymization-ca), in order to protect the user’s privacy, or to assist [Relying Parties](#relying-party) with attestation verification in a heterogeneous ecosystem.

Note: There is no guarantee that the [Relying Party](#relying-party) will obtain a verifiable [attestation statement](#attestation-statement) in this case. For example, in the case that the authenticator employs [self attestation](#self-attestation) and the [client](#client) passes the [attestation statement](#attestation-statement) through unmodified.

`direct`

The [Relying Party](#relying-party) wants to receive the [attestation statement](#attestation-statement) as generated by the [authenticator](#authenticator).

`enterprise`

The [Relying Party](#relying-party) wants to receive an enterprise attestation, which is an [attestation statement](#attestation-statement) that may include information which uniquely identifies the authenticator. This is intended for controlled deployments within an enterprise where the organization wishes to tie registrations to specific authenticators. User agents MUST NOT provide such an attestation unless the user agent or authenticator configuration permits it for the requested [RP ID](#rp-id).

If permitted, the user agent SHOULD signal to the authenticator (at [invocation time](#CreateCred-InvokeAuthnrMakeCred)) that enterprise attestation is requested, and convey the resulting [AAGUID](#aaguid) and [attestation statement](#attestation-statement), unaltered, to the [Relying Party](#relying-party).

### 5.5. Options for Assertion Generation (dictionary PublicKeyCredentialRequestOptions)

The `PublicKeyCredentialRequestOptions` dictionary supplies `get()` with the data it needs to generate an assertion. Its `challenge` member MUST be present, while its other members are OPTIONAL.

```
dictionary PublicKeyCredentialRequestOptions {
    required BufferSource                challenge;
    unsigned long                        timeout;
    DOMString                            rpId;
    sequence<PublicKeyCredentialDescriptor> allowCredentials = [];
    DOMString                            userVerification = "preferred";
    sequence<DOMString>                  hints = [];
    AuthenticationExtensionsClientInputs extensions;
};
```

`challenge`, of type [BufferSource](https://webidl.spec.whatwg.org/#BufferSource)

This member specifies a challenge that the [authenticator](#authenticator) signs, along with other data, when producing an [authentication assertion](#authentication-assertion). See the [§ 13.4.3 Cryptographic Challenges](#sctn-cryptographic-challenges) security consideration.

`timeout`, of type [unsigned long](https://webidl.spec.whatwg.org/#idl-unsigned-long)

This OPTIONAL member specifies a time, in milliseconds, that the [Relying Party](#relying-party) is willing to wait for the call to complete. The value is treated as a hint, and MAY be overridden by the [client](#client).

`rpId`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

This OPTIONAL member specifies the [RP ID](#rp-id) claimed by the [Relying Party](#relying-party). The [client](#client) MUST verify that the [Relying Party](#relying-party) ’s [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) matches the [scope](#scope) of this [RP ID](#rp-id). The [authenticator](#authenticator) MUST verify that this [RP ID](#rp-id) exactly equals the [rpId](#public-key-credential-source-rpid) of the [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) to be used for the [authentication ceremony](#authentication-ceremony).

If not specified, its value will be the `CredentialsContainer` object’s ’s [origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-settings-object-origin) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain).

`allowCredentials`, of type sequence< [PublicKeyCredentialDescriptor](#dictdef-publickeycredentialdescriptor) >, defaulting to `[]`

This OPTIONAL member is used by the [client](#client) to find [authenticators](#authenticator) eligible for this [authentication ceremony](#authentication-ceremony). It can be used in two ways:

- If the [user account](#user-account) to authenticate is already identified (e.g., if the user has entered a username), then the [Relying Party](#relying-party) SHOULD use this member to list [credential descriptors for credential records](#credential-descriptor-for-a-credential-record) in the [user account](#user-account). This SHOULD usually include all [credential records](#credential-record) in the [user account](#user-account).
	The [items](https://infra.spec.whatwg.org/#list-item) SHOULD specify `transports` whenever possible. This helps the [client](#client) optimize the user experience for any given situation. Also note that the [Relying Party](#relying-party) does not need to filter the list when requesting [user verification](#user-verification) — the [client](#client) will automatically ignore non-eligible credentials if `userVerification` is set to `required`.
	See also the [§ 14.6.3 Privacy leak via credential IDs](#sctn-credential-id-privacy-leak) privacy consideration.
- If the [user account](#user-account) to authenticate is not already identified, then the [Relying Party](#relying-party) MAY leave this member [empty](https://infra.spec.whatwg.org/#list-empty) or unspecified. In this case, only [discoverable credentials](#discoverable-credential) will be utilized in this [authentication ceremony](#authentication-ceremony), and the [user account](#user-account) MAY be identified by the `userHandle` of the resulting `AuthenticatorAssertionResponse`. If the available [authenticators](#authenticator) [contain](#contains) more than one [discoverable credential](#discoverable-credential) [scoped](#scope) to the [Relying Party](#relying-party), the credentials are displayed by the [client platform](#client-platform) or [authenticator](#authenticator) for the user to select from (see [step 7](#authenticatorGetAssertion-prompt-select-credential) of [§ 6.3.3 The authenticatorGetAssertion Operation](#sctn-op-get-assertion)).

If not [empty](https://infra.spec.whatwg.org/#list-empty), the client MUST return an error if none of the listed credentials can be used.

The list is ordered in descending order of preference: the first item in the list is the most preferred credential, and the last is the least preferred.

`userVerification`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString), defaulting to `"preferred"`

This OPTIONAL member specifies the [Relying Party](#relying-party) ’s requirements regarding [user verification](#user-verification) for the `get()` operation. The value SHOULD be a member of `UserVerificationRequirement` but [client platforms](#client-platform) MUST ignore unknown values, treating an unknown value as if the [member does not exist](https://infra.spec.whatwg.org/#map-exists). Eligible authenticators are filtered to only those capable of satisfying this requirement.

See `UserVerificationRequirement` for the description of `userVerification` ’s values and semantics.

`hints`, of type sequence< [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString) >, defaulting to `[]`

This OPTIONAL member contains zero or more elements from `PublicKeyCredentialHint` to guide the user agent in interacting with the user. Note that the elements have type `DOMString` despite being taken from that enumeration. See [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

`extensions`, of type [AuthenticationExtensionsClientInputs](#dictdef-authenticationextensionsclientinputs)

The [Relying Party](#relying-party) MAY use this OPTIONAL member to provide [client extension inputs](#client-extension-input) requesting additional processing by the [client](#client) and [authenticator](#authenticator).

The extensions framework is defined in [§ 9 WebAuthn Extensions](#sctn-extensions). Some extensions are defined in [§ 10 Defined Extensions](#sctn-defined-extensions); consult the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)") for an up-to-date list of registered [WebAuthn Extensions](#webauthn-extensions).

### 5.6. Abort Operations with AbortSignal

Developers are encouraged to leverage the `AbortController` to manage the `[[Create]](origin, options, sameOriginWithAncestors)` and `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` operations. See [DOM § 3.3 Using AbortController and AbortSignal objects in APIs](https://dom.spec.whatwg.org/#abortcontroller-api-integration) section for detailed instructions.

Note: [DOM § 3.3 Using AbortController and AbortSignal objects in APIs](https://dom.spec.whatwg.org/#abortcontroller-api-integration) section specifies that web platform APIs integrating with the `AbortController` must reject the promise immediately once the `AbortSignal` is [aborted](https://dom.spec.whatwg.org/#abortsignal-aborted). Given the complex inheritance and parallelization structure of the `[[Create]](origin, options, sameOriginWithAncestors)` and `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` methods, the algorithms for the two APIs fulfills this requirement by checking the [aborted](https://dom.spec.whatwg.org/#abortsignal-aborted) property in three places. In the case of `[[Create]](origin, options, sameOriginWithAncestors)`, the [aborted](https://dom.spec.whatwg.org/#abortsignal-aborted) property is checked first in [Credential Management 1 § 2.5.4 Create a Credential](https://w3c.github.io/webappsec-credential-management/#algorithm-create) immediately before calling `[[Create]](origin, options, sameOriginWithAncestors)`, then in [§ 5.1.3 Create a New Credential - PublicKeyCredential’s \[\[Create\]\](origin, options, sameOriginWithAncestors) Internal Method](#sctn-createCredential) right before [authenticator sessions](#authenticator-session) start, and finally during [authenticator sessions](#authenticator-session). The same goes for `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)`.

The [visibility](https://html.spec.whatwg.org/multipage/interaction.html#visibility-state) and [focus](https://www.w3.org/TR/CSS2/ui.html#x8) state of the `Window` object determines whether the `[[Create]](origin, options, sameOriginWithAncestors)` and `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` operations should continue. When the `Window` object associated with the [Document](https://dom.spec.whatwg.org/#concept-document) loses focus, `[[Create]](origin, options, sameOriginWithAncestors)` and `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` operations SHOULD be aborted.

### 5.7. WebAuthn Extensions Inputs and Outputs

The subsections below define the data types used for conveying [WebAuthn extension](#webauthn-extensions) inputs and outputs.

Note: [Authenticator extension outputs](#authenticator-extension-output) are conveyed as a part of [authenticator data](#authenticator-data) (see [Table 1](#table-authData)).

Note: The types defined below — `AuthenticationExtensionsClientInputs` and `AuthenticationExtensionsClientOutputs` — are applicable to both [registration extensions](#registration-extension) and [authentication extensions](#authentication-extension). The "Authentication..." portion of their names should be regarded as meaning "WebAuthentication..."

#### 5.7.1.

```
dictionary AuthenticationExtensionsClientInputs {
};
```

This is a dictionary containing the [client extension input](#client-extension-input) values for zero or more [WebAuthn Extensions](#webauthn-extensions).

#### 5.7.2.

```
dictionary AuthenticationExtensionsClientOutputs {
};
```

This is a dictionary containing the [client extension output](#client-extension-output) values for zero or more [WebAuthn Extensions](#webauthn-extensions).

#### 5.7.3. Authentication Extensions Authenticator Inputs (CDDL type AuthenticationExtensionsAuthenticatorInputs)

```
AuthenticationExtensionsAuthenticatorInputs = {
  * $$extensionInput
} .within { * tstr => any }
```

The [CDDL](#cddl) type `AuthenticationExtensionsAuthenticatorInputs` defines a [CBOR](#cbor) map containing the [authenticator extension input](#authenticator-extension-input) values for zero or more [WebAuthn Extensions](#webauthn-extensions). Extensions can add members as described in [§ 9.3 Extending Request Parameters](#sctn-extension-request-parameters).

This type is not exposed to the [Relying Party](#relying-party), but is used by the [client](#client) and [authenticator](#authenticator).

#### 5.7.4. Authentication Extensions Authenticator Outputs (CDDL type AuthenticationExtensionsAuthenticatorOutputs)

```
AuthenticationExtensionsAuthenticatorOutputs = {
  * $$extensionOutput
} .within { * tstr => any }
```

The [CDDL](#cddl) type `AuthenticationExtensionsAuthenticatorOutputs` defines a [CBOR](#cbor) map containing the [authenticator extension output](#authenticator-extension-output) values for zero or more [WebAuthn Extensions](#webauthn-extensions). Extensions can add members as described in [§ 9.3 Extending Request Parameters](#sctn-extension-request-parameters).

#### 5.7.5. Authentication Extensions Unsigned Authenticator Outputs (CDDL type AuthenticationExtensionsUnsignedAuthenticatorOutputs)

```
AuthenticationExtensionsUnsignedAuthenticatorOutputs = {
  * $$unsignedExtensionOutput
} .within { * tstr => any }
```

The [CDDL](#cddl) type `AuthenticationExtensionsUnsignedAuthenticatorOutputs` defines a [CBOR](#cbor) map containing the [unsigned extension output](#unsigned-extension-outputs) values for zero or more [WebAuthn Extensions](#webauthn-extensions). Extensions can add members as described in [§ 9.3 Extending Request Parameters](#sctn-extension-request-parameters).

### 5.8. Supporting Data Structures

The [public key credential](#public-key-credential) type uses certain data structures that are specified in supporting specifications. These are as follows.

#### 5.8.1.

The client data represents the contextual bindings of both the [WebAuthn Relying Party](#webauthn-relying-party) and the [client](#client). It is a key-value mapping whose keys are strings. Values can be any type that has a valid encoding in JSON. Its structure is defined by the following Web IDL.

Note: The `CollectedClientData` may be extended in the future. Therefore it’s critical when parsing to be tolerant of unknown keys and of any reordering of the keys. See also [§ 5.8.1.2 Limited Verification Algorithm](#clientdatajson-verification).

```
dictionary CollectedClientData {
    required DOMString           type;
    required DOMString           challenge;
    required DOMString           origin;
    boolean                      crossOrigin;
    DOMString                    topOrigin;
};

dictionary TokenBinding {
    required DOMString status;
    DOMString id;
};

enum TokenBindingStatus { "present", "supported" };
```

`type`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

This member contains the string "webauthn.create" when creating new credentials, and "webauthn.get" when getting an assertion from an existing credential. The purpose of this member is to prevent certain types of signature confusion attacks (where an attacker substitutes one legitimate signature for another).

`challenge`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

This member contains the base64url encoding of the challenge provided by the [Relying Party](#relying-party). See the [§ 13.4.3 Cryptographic Challenges](#sctn-cryptographic-challenges) security consideration.

`origin`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

This member contains the fully qualified [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) of the requester, as provided to the authenticator by the client, in the syntax defined by [\[RFC6454\]](#biblio-rfc6454 "The Web Origin Concept").

`crossOrigin`, of type [boolean](https://webidl.spec.whatwg.org/#idl-boolean)

This OPTIONAL member contains the inverse of the `sameOriginWithAncestors` argument value that was passed into the [internal method](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots).

`topOrigin`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

This OPTIONAL member contains the fully qualified [top-level origin](https://html.spec.whatwg.org/multipage/webappapis.html#concept-environment-top-level-origin) of the requester, in the syntax defined by [\[RFC6454\]](#biblio-rfc6454 "The Web Origin Concept"). It is set only if the call was made from context that is not [same-origin with its ancestors](https://w3c.github.io/webappsec-credential-management/#same-origin-with-its-ancestors), i.e. if `crossOrigin` is `true`.

\[RESERVED\] tokenBinding

This OPTIONAL member contains information about the state of the [Token Binding](https://tools.ietf.org/html/rfc8471#section-1) protocol [\[TokenBinding\]](#biblio-tokenbinding "The Token Binding Protocol Version 1.0") used when communicating with the [Relying Party](#relying-party). Its absence indicates that the client doesn’t support token binding

Note: While [Token Binding](https://tools.ietf.org/html/rfc8471#section-1) was present in Level 1 and Level 2 of WebAuthn, its use is not expected in Level 3. The [tokenBinding](#dom-collectedclientdata-tokenbinding) field is reserved so that it will not be reused for a different purpose.

`status`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

This member SHOULD be a member of `TokenBindingStatus` but [client platforms](#client-platform) MUST ignore unknown values, treating an unknown value as if the [tokenBinding](#dom-collectedclientdata-tokenbinding) [member does not exist](https://infra.spec.whatwg.org/#map-exists). When known, this member is one of the following:

`supported`

Indicates the client supports token binding, but it was not negotiated when communicating with the [Relying Party](#relying-party).

`present`

Indicates token binding was used when communicating with the [Relying Party](#relying-party). In this case, the `id` member MUST be present.

Note: The `TokenBindingStatus` enumeration is deliberately not referenced, see [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

`id`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

This member MUST be present if `status` is `present`, and MUST be a [base64url encoding](#base64url-encoding) of the [Token Binding ID](https://tools.ietf.org/html/rfc8471#section-3.2) that was used when communicating with the [Relying Party](#relying-party).

Note: Obtaining a [Token Binding ID](https://tools.ietf.org/html/rfc8471#section-3.2) is a [client platform](#client-platform) -specific operation.

The `CollectedClientData` structure is used by the client to compute the following quantities:

JSON-compatible serialization of client data

This is the result of performing the [JSON-compatible serialization algorithm](#clientdatajson-serialization) on the `CollectedClientData` dictionary.

Hash of the serialized client data

This is the hash (computed using SHA-256) of the [JSON-compatible serialization of client data](#collectedclientdata-json-compatible-serialization-of-client-data), as constructed by the client.

##### 5.8.1.1. Serialization

The serialization of the `CollectedClientData` is a subset of the algorithm for [JSON-serializing to bytes](https://infra.spec.whatwg.org/#serialize-a-javascript-value-to-json-bytes). I.e. it produces a valid JSON encoding of the `CollectedClientData` but also provides additional structure that may be exploited by verifiers to avoid integrating a full JSON parser. While verifiers are recommended to perform standard JSON parsing, they may use the [more limited algorithm](#clientdatajson-verification) below in contexts where a full JSON parser is too large. This verification algorithm requires only [base64url encoding](#base64url-encoding), appending of bytestrings (which could be implemented by writing into a fixed template), and simple conditional checks (assuming that inputs are known not to need escaping).

The serialization algorithm works by appending successive byte strings to an, initially empty, partial result until the complete result is obtained.

1. Let result be an empty byte string.
2. Append 0x7b2274797065223a (`{"type":`) to result.
3. Append [CCDToString](#ccdtostring) (`type`) to result.
4. Append 0x2c226368616c6c656e6765223a (`,"challenge":`) to result.
5. Append [CCDToString](#ccdtostring) (`challenge`) to result.
6. Append 0x2c226f726967696e223a (`,"origin":`) to result.
7. Append [CCDToString](#ccdtostring) (`origin`) to result.
8. Append 0x2c2263726f73734f726967696e223a (`,"crossOrigin":`) to result.
9. If `crossOrigin` is not present, or is `false`:
	1. Append 0x66616c7365 (`false`) to result.
10. Otherwise:
	1. Append 0x74727565 (`true`) to result.
11. If `topOrigin` is present:
	1. Append 0x2c22746f704f726967696e223a (`,"topOrigin":`) to result.
		2. Append [CCDToString](#ccdtostring) (`topOrigin`) to result.
12. Create a temporary copy of the `CollectedClientData` and remove the fields `type`, `challenge`, `origin`, `crossOrigin` (if present), and `topOrigin` (if present).
13. If no fields remain in the temporary copy then:
	1. Append 0x7d (`}`) to result.
14. Otherwise:
	1. Invoke [serialize JSON to bytes](https://infra.spec.whatwg.org/#serialize-a-javascript-value-to-json-bytes) on the temporary copy to produce a byte string remainder.
		2. Append 0x2c (`,`) to result.
		3. Remove the leading byte from remainder.
		4. Append remainder to result.
15. The result of the serialization is the value of result.

The function CCDToString is used in the above algorithm and is defined as:

1. Let encoded be an empty byte string.
2. Append 0x22 (`"`) to encoded.
3. Invoke [ToString](https://tc39.es/ecma262/#sec-tostring) on the given object to convert to a string.
4. For each code point in the resulting string, if the code point:
	is in the set {U+0020, U+0021, U+0023–U+005B, U+005D–U+10FFFF}
	Append the UTF-8 encoding of that code point to encoded.
	is U+0022
	Append 0x5c22 (`\"`) to encoded.
	is U+005C
	Append 0x5c5c (\\\\) to encoded.
	otherwise
	Append 0x5c75 (`\u`) to encoded, followed by four, lower-case hex digits that, when interpreted as a base-16 number, represent that code point.
5. Append 0x22 (`"`) to encoded.
6. The result of this function is the value of encoded.

##### 5.8.1.2. Limited Verification Algorithm

Verifiers may use the following algorithm to verify an encoded `CollectedClientData` if they cannot support a full JSON parser:

1. The inputs to the algorithm are:
	1. A bytestring, clientDataJSON, that contains `clientDataJSON`  — the serialized `CollectedClientData` that is to be verified.
		2. A string, type, that contains the expected `type`.
		3. A byte string, challenge, that contains the challenge byte string that was given in the `PublicKeyCredentialRequestOptions` or `PublicKeyCredentialCreationOptions`.
		4. A string, origin, that contains the expected `origin` that issued the request to the user agent.
		5. An optional string, topOrigin, that contains the expected `topOrigin` that issued the request to the user agent, if available.
		6. A boolean, requireTopOrigin, that is true if, and only if, the verification should fail if topOrigin is defined and the `topOrigin` attribute is not present in clientDataJSON.
		This means that the verification algorithm is backwards compatible with the [JSON-compatible serialization algorithm](https://www.w3.org/TR/2021/REC-webauthn-2-20210408/#clientdatajson-serialization) in Web Authentication Level 2 [\[webauthn-2-20210408\]](#biblio-webauthn-2-20210408 "Web Authentication: An API for accessing Public Key Credentials - Level 2") if, and only if, requireTopOrigin is `false`.
2. Let expected be an empty byte string.
3. Append 0x7b2274797065223a (`{"type":`) to expected.
4. Append [CCDToString](#ccdtostring) (type) to expected.
5. Append 0x2c226368616c6c656e6765223a (`,"challenge":`) to expected.
6. Perform [base64url encoding](#base64url-encoding) on challenge to produce a string, challengeBase64.
7. Append [CCDToString](#ccdtostring) (challengeBase64) to expected.
8. Append 0x2c226f726967696e223a (`,"origin":`) to expected.
9. Append [CCDToString](#ccdtostring) (origin) to expected.
10. Append 0x2c2263726f73734f726967696e223a (`,"crossOrigin":`) to expected.
11. If topOrigin is defined:
	1. Append 0x74727565 (`true`) to expected.
		2. If requireTopOrigin is true or if 0x2c22746f704f726967696e223a (`,"topOrigin":`) is a prefix of the substring of clientDataJSON beginning at the offset equal to the length of expected:
		1. Append 0x2c22746f704f726967696e223a (`,"topOrigin":`) to expected.
				2. Append [CCDToString](#ccdtostring) (topOrigin) to expected.
12. Otherwise, i.e. topOrigin is not defined:
	1. Append 0x66616c7365 (`false`) to expected.
13. If expected is not a prefix of clientDataJSON then the verification has failed.
14. If clientDataJSON is not at least one byte longer than expected then the verification has failed.
15. If the byte of clientDataJSON at the offset equal to the length of expected:
	is 0x7d
	The verification is successful.
	is 0x2c
	The verification is successful.
	otherwise
	The verification has failed.

##### 5.8.1.3. Future development

In order to remain compatible with the [limited verification algorithm](#clientdatajson-verification), future versions of this specification must not remove any of the fields `type`, `challenge`, `origin`, `crossOrigin`, or `topOrigin` from `CollectedClientData`. They also must not change the [serialization algorithm](#clientdatajson-verification) to change the order in which those fields are serialized, or insert new fields between them.

If additional fields are added to `CollectedClientData` then verifiers that employ the [limited verification algorithm](#clientdatajson-verification) will not be able to consider them until the two algorithms above are updated to include them. Once such an update occurs then the added fields inherit the same limitations as described in the previous paragraph. Such an algorithm update would have to accomodate serializations produced by previous versions. I.e. the verification algorithm would have to handle the fact that a sixth key–value pair may not appear sixth (or at all) if generated by a user agent working from a previous version.

#### 5.8.2. Credential Type Enumeration (enum PublicKeyCredentialType)

```
enum PublicKeyCredentialType {
    "public-key"
};
```

Note: The `PublicKeyCredentialType` enumeration is deliberately not referenced, see [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

This enumeration defines the valid credential types. It is an extension point; values can be added to it in the future, as more credential types are defined. The values of this enumeration are used for versioning the Authentication Assertion and attestation structures according to the type of the authenticator.

Currently one credential type is defined, namely " `public-key` ".

#### 5.8.3. Credential Descriptor (dictionary PublicKeyCredentialDescriptor)

```
dictionary PublicKeyCredentialDescriptor {
    required DOMString                    type;
    required BufferSource                 id;
    sequence<DOMString>                   transports;
};
```

This dictionary identifies a specific [public key credential](#public-key-credential). It is used in `create()` to prevent creating duplicate credentials on the same [authenticator](#authenticator), and in `get()` to determine if and how the credential can currently be reached by the [client](#client).

The [credential descriptor for a credential record](#credential-descriptor-for-a-credential-record) is a subset of the properties of that [credential record](#credential-record), and mirrors some fields of the `PublicKeyCredential` object returned by `create()` and `get()`.

`type`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

This member contains the type of the [public key credential](#public-key-credential) the caller is referring to. The value SHOULD be a member of `PublicKeyCredentialType` but [client platforms](#client-platform) MUST ignore any `PublicKeyCredentialDescriptor` with an unknown `type`. However, if all elements are ignored due to unknown `type`, then that MUST result in an error since an empty `allowCredentials` is semantically distinct.

This SHOULD be set to the value of the [type](#abstract-opdef-credential-record-type) item of the [credential record](#credential-record) representing the identified [public key credential source](#public-key-credential-source). This mirrors the `type` field of `PublicKeyCredential`.

`id`, of type [BufferSource](https://webidl.spec.whatwg.org/#BufferSource)

This member contains the [credential ID](#credential-id) of the [public key credential](#public-key-credential) the caller is referring to.

This SHOULD be set to the value of the [id](#abstract-opdef-credential-record-id) item of the [credential record](#credential-record) representing the identified [public key credential source](#public-key-credential-source). This mirrors the `rawId` field of `PublicKeyCredential`.

`transports`, of type sequence< [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString) >

This OPTIONAL member contains a hint as to how the [client](#client) might communicate with the [managing authenticator](#public-key-credential-source-managing-authenticator) of the [public key credential](#public-key-credential) the caller is referring to. The values SHOULD be members of `AuthenticatorTransport` but [client platforms](#client-platform) MUST ignore unknown values.

This SHOULD be set to the value of the [transports](#abstract-opdef-credential-record-transports) item of the [credential record](#credential-record) representing the identified [public key credential source](#public-key-credential-source). This mirrors the `` `response`.`getTransports()` `` method of the `PublicKeyCredential` structure created by a `create()` operation.

#### 5.8.4. Authenticator Transport Enumeration (enum AuthenticatorTransport)

```
enum AuthenticatorTransport {
    "usb",
    "nfc",
    "ble",
    "smart-card",
    "hybrid",
    "internal"
};
```

Note: The `AuthenticatorTransport` enumeration is deliberately not referenced, see [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

[Authenticators](#authenticator) may implement various [transports](#enum-transport) for communicating with [clients](#client). This enumeration defines hints as to how clients might communicate with a particular authenticator in order to obtain an assertion for a specific credential. Note that these hints represent the [WebAuthn Relying Party](#webauthn-relying-party) ’s best belief as to how an authenticator may be reached. A [Relying Party](#relying-party) will typically learn of the supported transports for a [public key credential](#public-key-credential) via `getTransports()`.

`usb`

Indicates the respective [authenticator](#authenticator) can be contacted over removable USB.

`nfc`

Indicates the respective [authenticator](#authenticator) can be contacted over Near Field Communication (NFC).

`ble`

Indicates the respective [authenticator](#authenticator) can be contacted over Bluetooth Smart (Bluetooth Low Energy / BLE).

`smart-card`

Indicates the respective [authenticator](#authenticator) can be contacted over ISO/IEC 7816 smart card with contacts.

`hybrid`

Indicates the respective [authenticator](#authenticator) can be contacted using a combination of (often separate) data-transport and proximity mechanisms. This supports, for example, authentication on a desktop computer using a smartphone.

`internal`

Indicates the respective [authenticator](#authenticator) is contacted using a [client device](#client-device) -specific transport, i.e., it is a [platform authenticator](#platform-authenticators). These authenticators are not removable from the [client device](#client-device).

#### 5.8.5.

```
typedef long COSEAlgorithmIdentifier;
```

A `COSEAlgorithmIdentifier` ’s value is a number identifying a cryptographic algorithm. The algorithm identifiers SHOULD be values registered in the IANA COSE Algorithms registry [\[IANA-COSE-ALGS-REG\]](#biblio-iana-cose-algs-reg "IANA CBOR Object Signing and Encryption (COSE) Algorithms Registry"), for instance, `-7` for "ES256" and `-257` for "RS256".

The COSE algorithms registry leaves degrees of freedom to be specified by other parameters in a [COSE key](https://tools.ietf.org/html/rfc9052#name-key-objects). In order to promote interoperability, this specification makes the following additional guarantees of [credential public keys](#credential-public-key):

1. Keys with algorithm -7 (ES256) MUST specify 1 (P-256) as the [crv](https://tools.ietf.org/html/rfc9053#name-double-coordinate-curves) parameter and MUST NOT use the compressed point form.
2. Keys with algorithm -9 (ESP256) MUST NOT use the compressed point form.
3. Keys with algorithm -35 (ES384) MUST specify 2 (P-384) as the [crv](https://tools.ietf.org/html/rfc9053#name-double-coordinate-curves) parameter and MUST NOT use the compressed point form.
4. Keys with algorithm -51 (ESP384) MUST NOT use the compressed point form.
5. Keys with algorithm -36 (ES512) MUST specify 3 (P-521) as the [crv](https://tools.ietf.org/html/rfc9053#name-double-coordinate-curves) parameter and MUST NOT use the compressed point form.
6. Keys with algorithm -52 (ESP512) MUST NOT use the compressed point form.
7. Keys with algorithm -8 (EdDSA) MUST specify 6 (Ed25519) as the [crv](https://tools.ietf.org/html/rfc9053#name-double-coordinate-curves) parameter. (These always use a compressed form in COSE.)

These restrictions align with the recommendation in [Section 2.1](https://tools.ietf.org/html/rfc9053#section-2.1) of [\[RFC9053\]](#biblio-rfc9053 "CBOR Object Signing and Encryption (COSE): Initial Algorithms").

Note: There are many checks neccessary to correctly implement signature verification using these algorithms. One of these is that, when processing uncompressed elliptic-curve points, implementations should check that the point is actually on the curve. This check is highlighted because it’s judged to be at particular risk of falling through the gap between a cryptographic library and other code.

#### 5.8.6. User Verification Requirement Enumeration (enum UserVerificationRequirement)

```
enum UserVerificationRequirement {
    "required",
    "preferred",
    "discouraged"
};
```

A [WebAuthn Relying Party](#webauthn-relying-party) may require [user verification](#user-verification) for some of its operations but not for others, and may use this type to express its needs.

Note: The `UserVerificationRequirement` enumeration is deliberately not referenced, see [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

`required`

The [Relying Party](#relying-party) requires [user verification](#user-verification) for the operation and will fail the overall [ceremony](#ceremony) if the response does not have the [UV](#authdata-flags-uv) [flag](#authdata-flags) set. The [client](#client) MUST return an error if [user verification](#user-verification) cannot be performed.

`preferred`

The [Relying Party](#relying-party) prefers [user verification](#user-verification) for the operation if possible, but will not fail the operation if the response does not have the [UV](#authdata-flags-uv) [flag](#authdata-flags) set.

`discouraged`

The [Relying Party](#relying-party) does not want [user verification](#user-verification) employed during the operation (e.g., in the interest of minimizing disruption to the user interaction flow).

#### 5.8.7. Client Capability Enumeration (enum ClientCapability)

```
enum ClientCapability {
    "conditionalCreate",
    "conditionalGet",
    "hybridTransport",
    "passkeyPlatformAuthenticator",
    "userVerifyingPlatformAuthenticator",
    "relatedOrigins",
    "signalAllAcceptedCredentials",
    "signalCurrentUserDetails",
    "signalUnknownCredential"
};
```

This enumeration defines a limited set of client capabilities which a [WebAuthn Relying Party](#webauthn-relying-party) may evaluate to offer certain workflows and experiences to users.

[Relying Parties](#relying-party) may use the `getClientCapabilities()` method of `PublicKeyCredential` to obtain a description of available capabilities.

Note: The `ClientCapability` enumeration is deliberately not referenced, see [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

`conditionalCreate`

The [WebAuthn Client](#webauthn-client) is capable of `conditional` mediation for [registration ceremonies](#registration-ceremony)..

See [§ 5.1.3 Create a New Credential - PublicKeyCredential’s \[\[Create\]\](origin, options, sameOriginWithAncestors) Internal Method](#sctn-createCredential) for more details.

`conditionalGet`

The [WebAuthn Client](#webauthn-client) is capable of `conditional` mediation for [authentication ceremonies](#authentication-ceremony).

This capability is equivalent to `isConditionalMediationAvailable()` resolving to `true`.

See [§ 5.1.4 Use an Existing Credential to Make an Assertion](#sctn-getAssertion) for more details.

`hybridTransport`

The [WebAuthn Client](#webauthn-client) supports usage of the `hybrid` transport.

`passkeyPlatformAuthenticator`

The [WebAuthn Client](#webauthn-client) supports usage of a [passkey platform authenticator](#passkey-platform-authenticator), locally and/or via `hybrid` transport.

`userVerifyingPlatformAuthenticator`

The [WebAuthn Client](#webauthn-client) supports usage of a [user-verifying platform authenticator](#user-verifying-platform-authenticator).

The [WebAuthn Client](#webauthn-client) supports [Related Origin Requests](#sctn-related-origins).

`signalAllAcceptedCredentials`

The [WebAuthn Client](#webauthn-client) supports `signalAllAcceptedCredentials()`.

`signalCurrentUserDetails`,

The [WebAuthn Client](#webauthn-client) supports `signalCurrentUserDetails()`.

`signalUnknownCredential`

The [WebAuthn Client](#webauthn-client) supports `signalUnknownCredential()`.

#### 5.8.8. User-agent Hints Enumeration (enum PublicKeyCredentialHint)

```
enum PublicKeyCredentialHint {
    "security-key",
    "client-device",
    "hybrid",
};
```

Note: The `PublicKeyCredentialHint` enumeration is deliberately not referenced, see [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).

[WebAuthn Relying Parties](#webauthn-relying-party) may use this enumeration to communicate hints to the user-agent about how a request may be best completed. These hints are not requirements, and do not bind the user-agent, but may guide it in providing the best experience by using contextual information that the [Relying Party](#relying-party) has about the request. Hints are provided in order of decreasing preference so, if two hints are contradictory, the first one controls. Hints may also overlap: if a more-specific hint is defined a [Relying Party](#relying-party) may still wish to send less specific ones for user-agents that may not recognise the more specific one. In this case the most specific hint should be sent before the less-specific ones. If the same hint appears more than once, its second and later appearences are ignored.

Hints MAY contradict information contained in credential `transports` and `authenticatorAttachment`. When this occurs, the hints take precedence. (Note that `transports` values are not provided when using [discoverable credentials](#discoverable-credential), leaving hints as the only avenue for expressing some aspects of such a request.)

`security-key`

Indicates that the [Relying Party](#relying-party) believes that users will satisfy this request with a physical security key. For example, an enterprise [Relying Party](#relying-party) may set this hint if they have issued security keys to their employees and will only accept those [authenticators](#authenticator) for [registration](#registration-ceremony) and [authentication](#authentication-ceremony).

For compatibility with older user agents, when this hint is used in `PublicKeyCredentialCreationOptions`, the `authenticatorAttachment` SHOULD be set to `cross-platform`.

`client-device`

Indicates that the [Relying Party](#relying-party) believes that users will satisfy this request with a [platform authenticator](#platform-authenticators) attached to the [client device](#client-device).

For compatibility with older user agents, when this hint is used in `PublicKeyCredentialCreationOptions`, the `authenticatorAttachment` SHOULD be set to `platform`.

`hybrid`

Indicates that the [Relying Party](#relying-party) believes that users will satisfy this request with general-purpose [authenticators](#authenticator) such as smartphones. For example, a consumer [Relying Party](#relying-party) may believe that only a small fraction of their customers possesses dedicated security keys. This option also implies that the local [platform authenticator](#platform-authenticators) should not be promoted in the UI.

For compatibility with older user agents, when this hint is used in `PublicKeyCredentialCreationOptions`, the `authenticatorAttachment` SHOULD be set to `cross-platform`.

### 5.9. Permissions Policy integration

This specification defines two [policy-controlled features](https://w3c.github.io/webappsec-permissions-policy/#policy-controlled-feature) identified by the feature-identifier tokens " `publickey-credentials-create` " and " `publickey-credentials-get` ". Their [default allowlists](https://w3c.github.io/webappsec-permissions-policy/#policy-controlled-feature-default-allowlist) are both ' `self` '. [\[Permissions-Policy\]](#biblio-permissions-policy "Permissions Policy")

A `Document` ’s [permissions policy](https://html.spec.whatwg.org/multipage/dom.html#concept-document-permissions-policy) determines whether any content in that [document](https://html.spec.whatwg.org/multipage/dom.html#documents) is [allowed to successfully invoke](https://html.spec.whatwg.org/multipage/iframe-embed-object.html#allowed-to-use) the [Web Authentication API](#web-authentication-api), i.e., via `navigator.credentials.create({publicKey:..., ...})` or `navigator.credentials.get({publicKey:..., ...})` If disabled in any document, no content in the document will be [allowed to use](https://html.spec.whatwg.org/multipage/iframe-embed-object.html#allowed-to-use) the foregoing methods: attempting to do so will [return an error](https://www.w3.org/2001/tag/doc/promises-guide#errors).

Note: Algorithms specified in [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1") perform the actual permissions policy evaluation. This is because such policy evaluation needs to occur when there is access to the [current settings object](https://html.spec.whatwg.org/multipage/webappapis.html#current-settings-object). The `[[Create]](origin, options, sameOriginWithAncestors)` and `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` [internal methods](https://tc39.github.io/ecma262/#sec-object-internal-methods-and-internal-slots) do not have such access since they are invoked [in parallel](https://html.spec.whatwg.org/multipage/infrastructure.html#in-parallel) by `CredentialsContainer` ’s [Create a `Credential`](https://w3c.github.io/webappsec-credential-management/#abstract-opdef-create-a-credential) and [Request a `Credential`](https://w3c.github.io/webappsec-credential-management/#abstract-opdef-request-a-credential) abstract operations [\[CREDENTIAL-MANAGEMENT-1\]](#biblio-credential-management-1 "Credential Management Level 1").

### 5.10. Using Web Authentication within iframe elements

The [Web Authentication API](#web-authentication-api) is disabled by default in cross-origin `iframe` s. To override this default policy and indicate that a cross-origin `iframe` is allowed to invoke the [Web Authentication API](#web-authentication-api) ’s `[[Create]](origin, options, sameOriginWithAncestors)` and `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` methods, specify the `allow` attribute on the `iframe` element and include the `publickey-credentials-create` or `publickey-credentials-get` feature-identifier token, respectively, in the `allow` attribute’s value.

[Relying Parties](#relying-party) utilizing the WebAuthn API in an embedded context should review [§ 13.4.2 Visibility Considerations for Embedded Usage](#sctn-seccons-visibility) regarding [UI redressing](#ui-redressing) and its possible mitigations.

### 5.11. Using Web Authentication across related origins

By default, Web Authentication requires that the [RP ID](#rp-id) be equal to the [origin](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain), or a [registrable domain suffix](https://html.spec.whatwg.org/multipage/browsers.html#is-a-registrable-domain-suffix-of-or-is-equal-to) of the [origin](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) ’s [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain).

This can make deployment challenging for large environments where multiple country-specific domains are in use (e.g. example.com vs example.co.uk vs example.sg), where alternative or brand domains are required (e.g. myexampletravel.com vs examplecruises.com), and/or where platform as a service providers are used to support mobile apps.

[WebAuthn Relying Parties](#webauthn-relying-party) can opt in to allowing [WebAuthn Clients](#webauthn-client) to enable a credential to be created and used across a limited set of related [origins](https://html.spec.whatwg.org/multipage/origin.html#concept-origin). Such [Relying Parties](#relying-party) MUST choose a common [RP ID](#rp-id) to use across all ceremonies from related origins.

A JSON document MUST be hosted at the `webauthn` well-known URL [\[RFC8615\]](#biblio-rfc8615 "Well-Known Uniform Resource Identifiers (URIs)") for the [RP ID](#rp-id), and MUST be served using HTTPS. The JSON document MUST be returned as follows:

- The content type MUST be `application/json`.
- The top-level JSON object MUST contain a key named `origins` whose value MUST be an array of one or more strings containing web origins.

For example, for the RP ID `example.com`:

```json
{
    "origins": [
        "https://example.co.uk",
        "https://example.de",
        "https://example.sg",
        "https://example.net",
        "https://exampledelivery.com",
        "https://exampledelivery.co.uk",
        "https://exampledelivery.de",
        "https://exampledelivery.sg",
        "https://myexamplerewards.com",
        "https://examplecars.com"
    ]
}
```

[WebAuthn Clients](#webauthn-client) supporting this feature MUST support at least five [registrable origin labels](#registrable-origin-label). Client policy SHOULD define an upper limit to prevent abuse.

Requests to this well-known endpoint by [WebAuthn Clients](#webauthn-client) MUST be made without [credentials](https://fetch.spec.whatwg.org/#concept-request-credentials-mode), without a [referrer](https://fetch.spec.whatwg.org/#concept-request-referrer-policy), and using the `https:` [scheme](https://url.spec.whatwg.org/#concept-url-scheme). When following redirects, [WebAuthn Clients](#webauthn-client) MUST explicitly require all redirects to also use the `https:` [scheme](https://url.spec.whatwg.org/#concept-url-scheme).

[WebAuthn Clients](#webauthn-client) supporting this feature SHOULD include `relatedOrigins` in their response to [getClientCapabilities()](#sctn-getClientCapabilities).

#### 5.11.1. Validating Related Origins

The, given arguments callerOrigin and rpIdRequested, is as follows:

1. Let maxLabels be the maximum number of [registrable origin labels](#registrable-origin-label) allowed by client policy.
2. Fetch the `webauthn` well-known URL [\[RFC8615\]](#biblio-rfc8615 "Well-Known Uniform Resource Identifiers (URIs)") for the RP ID rpIdRequested (i.e., `https://rpIdRequested/.well-known/webauthn`) without [credentials](https://fetch.spec.whatwg.org/#concept-request-credentials-mode), without a [referrer](https://fetch.spec.whatwg.org/#concept-request-referrer-policy) and using the `https:` [scheme](https://url.spec.whatwg.org/#concept-url-scheme).
	1. If the fetch fails, the response does not have a content type of `application/json`, or does not have a status code (after following redirects) of 200, then throw a " `SecurityError` " `DOMException`.
		2. If the body of the resource is not a valid JSON object, then throw a " `SecurityError` " `DOMException`.
		3. If the value of the origins property of the JSON object is missing, or is not an array of strings, then throw a " `SecurityError` " `DOMException`.
3. Let labelsSeen be a new empty [set](https://infra.spec.whatwg.org/#ordered-set).
4. [For each](https://infra.spec.whatwg.org/#list-iterate) originItem of origins:
	1. Let url be the result of running the [URL parser](https://url.spec.whatwg.org/#concept-url-parser) with originItem as the input. If that fails, [continue](https://infra.spec.whatwg.org/#iteration-continue).
		2. Let domain be the [effective domain](https://html.spec.whatwg.org/multipage/browsers.html#concept-origin-effective-domain) of url. If that is null, [continue](https://infra.spec.whatwg.org/#iteration-continue).
		3. Let label be [registrable origin label](#registrable-origin-label) of domain.
		4. If label is empty or null, [continue](https://infra.spec.whatwg.org/#iteration-continue).
		5. If the [size](https://infra.spec.whatwg.org/#list-size) of labelsSeen is greater than or equal to maxLabels and labelsSeen does not [contain](https://infra.spec.whatwg.org/#list-contain) label, [continue](https://infra.spec.whatwg.org/#iteration-continue).
		6. If callerOrigin and url are [same origin](https://html.spec.whatwg.org/multipage/browsers.html#same-origin), return `true`.
		7. If the [size](https://infra.spec.whatwg.org/#list-size) of labelsSeen is less than maxLabels, [append](https://infra.spec.whatwg.org/#set-append) label to labelsSeen.
5. Return `false`.

## 6\. WebAuthn Authenticator Model

[The Web Authentication API](#sctn-api) implies a specific abstract functional model for a [WebAuthn Authenticator](#webauthn-authenticator). This section describes that [authenticator model](#authenticator-model).

[Client platforms](#client-platform) MAY implement and expose this abstract model in any way desired. However, the behavior of the client’s Web Authentication API implementation, when operating on the authenticators supported by that [client platform](#client-platform), MUST be indistinguishable from the behavior specified in [§ 5 Web Authentication API](#sctn-api).

Note: [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") is an example of a concrete instantiation of this model, but it is one in which there are differences in the data it returns and those expected by the [WebAuthn API](#sctn-api) ’s algorithms. The CTAP2 response messages are CBOR maps constructed using integer keys rather than the string keys defined in this specification for the same objects. The [client](#client) is expected to perform any needed transformations on such data. The [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") specification details the mapping between CTAP2 integer keys and WebAuthn string keys in Section [§6. Authenticator API](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticator-api).

For authenticators, this model defines the logical operations that they MUST support, and the data formats that they expose to the client and the [WebAuthn Relying Party](#webauthn-relying-party). However, it does not define the details of how authenticators communicate with the [client device](#client-device), unless they are necessary for interoperability with [Relying Parties](#relying-party). For instance, this abstract model does not define protocols for connecting authenticators to clients over transports such as USB or NFC. Similarly, this abstract model does not define specific error codes or methods of returning them; however, it does define error behavior in terms of the needs of the client. Therefore, specific error codes are mentioned as a means of showing which error conditions MUST be distinguishable (or not) from each other in order to enable a compliant and secure client implementation.

[Relying Parties](#relying-party) may influence authenticator selection, if they deem necessary, by stipulating various authenticator characteristics when [creating credentials](#sctn-createCredential) and/or when [generating assertions](#sctn-getAssertion), through use of [credential creation options](#dictionary-makecredentialoptions) or [assertion generation options](#dictionary-assertion-options), respectively. The algorithms underlying the [WebAuthn API](#sctn-api) marshal these options and pass them to the applicable [authenticator operations](#sctn-authenticator-ops) defined below.

In this abstract model, the authenticator provides key management and cryptographic signatures. It can be embedded in the WebAuthn client or housed in a separate device entirely. The authenticator itself can contain a cryptographic module which operates at a higher security level than the rest of the authenticator. This is particularly important for authenticators that are embedded in the WebAuthn client, as in those cases this cryptographic module (which may, for example, be a TPM) could be considered more trustworthy than the rest of the authenticator.

Each authenticator stores a credentials map, a [map](https://infra.spec.whatwg.org/#ordered-map) from ([rpId](#public-key-credential-source-rpid), [userHandle](#public-key-credential-source-userhandle)) to [public key credential source](#public-key-credential-source).

Additionally, each authenticator has an Authenticator Attestation Globally Unique Identifier or AAGUID, which is a 128-bit identifier indicating the type (e.g. make and model) of the authenticator. The AAGUID MUST be chosen by its maker to be identical across all substantially identical authenticators made by that maker, and different (with high probability) from the AAGUIDs of all other types of authenticators. The AAGUID for a given type of authenticator SHOULD be randomly generated to ensure this. The [Relying Party](#relying-party) MAY use the AAGUID to infer certain properties of the authenticator, such as certification level and strength of key protection, using information from other sources. The [Relying Party](#relying-party) MAY use the AAGUID to attempt to identify the maker of the authenticator without requesting and verifying [attestation](#attestation), but the AAGUID is not provably authentic without [attestation](#attestation).

The primary function of the authenticator is to provide [WebAuthn signatures](#webauthn-signature), which are bound to various contextual data. These data are observed and added at different levels of the stack as a signature request passes from the server to the authenticator. In verifying a signature, the server checks these bindings against expected values. These contextual bindings are divided in two: Those added by the [Relying Party](#relying-party) or the client, referred to as [client data](#client-data); and those added by the authenticator, referred to as the [authenticator data](#authenticator-data). The authenticator signs over the [client data](#client-data), but is otherwise not interested in its contents. To save bandwidth and processing requirements on the authenticator, the client hashes the [client data](#client-data) and sends only the result to the authenticator. The authenticator signs over the combination of the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data), and its own [authenticator data](#authenticator-data).

The goals of this design can be summarized as follows.

- The scheme for generating signatures should accommodate cases where the link between the [client device](#client-device) and authenticator is very limited, in bandwidth and/or latency. Examples include Bluetooth Low Energy and Near-Field Communication.
- The data processed by the authenticator should be small and easy to interpret in low-level code. In particular, authenticators should not have to parse high-level encodings such as JSON.
- Both the [client](#client) and the authenticator should have the flexibility to add contextual bindings as needed.
- The design aims to reuse as much as possible of existing encoding formats in order to aid adoption and implementation.

Authenticators produce cryptographic signatures for two distinct purposes:

1. An attestation signature is produced when a new [public key credential](#public-key-credential) is created via an [authenticatorMakeCredential](#authenticatormakecredential) operation. An [attestation signature](#attestation-signature) provides cryptographic proof of certain properties of the [authenticator](#authenticator) and the credential. For instance, an [attestation signature](#attestation-signature) asserts the [authenticator](#authenticator) type (as denoted by its AAGUID) and the [credential public key](#credential-public-key). The [attestation signature](#attestation-signature) is signed by an [attestation private key](#attestation-private-key), which is chosen depending on the type of [attestation](#attestation) desired. For more details on [attestation](#attestation), see [§ 6.5 Attestation](#sctn-attestation).
2. An assertion signature is produced when the [authenticatorGetAssertion](#authenticatorgetassertion) method is invoked. It represents an assertion by the [authenticator](#authenticator) that the user has to a specific transaction, such as logging in, or completing a purchase. Thus, an [assertion signature](#assertion-signature) asserts that the [authenticator](#authenticator) possessing a particular [credential private key](#credential-private-key) has established, to the best of its ability, that the user requesting this transaction is the same user who to creating that particular [public key credential](#public-key-credential). It also asserts additional information, termed [client data](#client-data), that may be useful to the caller, such as the means by which was provided, and the prompt shown to the user by the [authenticator](#authenticator). The [assertion signature](#assertion-signature) format is illustrated in [Figure 4, below](#fig-signature).

The term WebAuthn signature refers to both [attestation signatures](#attestation-signature) and [assertion signatures](#assertion-signature). The formats of these signatures, as well as the procedures for generating them, are specified below.

### 6.1. Authenticator Data

The authenticator data structure encodes contextual bindings made by the [authenticator](#authenticator). These bindings are controlled by the authenticator itself, and derive their trust from the [WebAuthn Relying Party](#webauthn-relying-party) ’s assessment of the security properties of the authenticator. In one extreme case, the authenticator may be embedded in the client, and its bindings may be no more trustworthy than the [client data](#client-data). At the other extreme, the authenticator may be a discrete entity with high-security hardware and software, connected to the client over a secure channel. In both cases, the [Relying Party](#relying-party) receives the [authenticator data](#authenticator-data) in the same format, and uses its knowledge of the authenticator to make trust decisions.

The [authenticator data](#authenticator-data) has a compact but extensible encoding. This is desired since authenticators can be devices with limited capabilities and low power requirements, with much simpler software stacks than the [client platform](#client-platform).

The [authenticator data](#authenticator-data) structure is a byte array of 37 bytes or more, laid out as shown in [Table](#table-authData) .

| Name | Length (in bytes) | Description |
| --- | --- | --- |
| rpIdHash | 32 | SHA-256 hash of the [RP ID](#rp-id) the [credential](#public-key-credential) is [scoped](#scope) to. |
| flags | 1 | Flags (bit 0 is the least significant bit): - Bit 0: [User Present](#concept-user-present) (UP) result. 	- `1` means the user is [present](#concept-user-present). 		- `0` means the user is not [present](#concept-user-present). - Bit 1: Reserved for future use (`RFU1`). - Bit 2: [User Verified](#concept-user-verified) (UV) result. 	- `1` means the user is [verified](#concept-user-verified). 		- `0` means the user is not [verified](#concept-user-verified). - Bit 3: [Backup Eligibility](#backup-eligibility) (BE). 	- `1` means the [public key credential source](#public-key-credential-source) is [backup eligible](#backup-eligible). 		- `0` means the [public key credential source](#public-key-credential-source) is not [backup eligible](#backup-eligible). - Bit 4: [Backup State](#backup-state) (BS). 	- `1` means the [public key credential source](#public-key-credential-source) is currently [backed up](#backed-up). 		- `0` means the [public key credential source](#public-key-credential-source) is not currently [backed up](#backed-up). - Bit 5: Reserved for future use (`RFU2`). - Bit 6: [Attested credential data](#attested-credential-data) included (AT). 	- Indicates whether the authenticator added [attested credential data](#attested-credential-data). - Bit 7: Extension data included (ED). 	- Indicates if the [authenticator data](#authenticator-data) has [extensions](#authdata-extensions). |
| signCount | 4 | [Signature counter](#signature-counter), 32-bit unsigned big-endian integer. |
| attestedCredentialData | variable (if present) | [attested credential data](#attested-credential-data) (if present). See [§ 6.5.1 Attested Credential Data](#sctn-attested-credential-data) for details. Its length depends on the [length](#authdata-attestedcredentialdata-credentialidlength) of the [credential ID](#authdata-attestedcredentialdata-credentialid) and [credential public key](#authdata-attestedcredentialdata-credentialpublickey) being attested. |
| extensions | variable (if present) | Extension-defined [authenticator data](#authenticator-data). This is a [CBOR](#cbor) [\[RFC8949\]](#biblio-rfc8949 "Concise Binary Object Representation (CBOR)") map with [extension identifiers](#extension-identifier) as keys, and [authenticator extension outputs](#authenticator-extension-output) as values. See [§ 9 WebAuthn Extensions](#sctn-extensions) for details. |

[Authenticator data](#authenticator-data) layout. The names in the Name column are only for reference within this document, and are not present in the actual representation of the [authenticator data](#authenticator-data).

The [RP ID](#rp-id) is originally received from the [client](#client) when the credential is created, and again when an [assertion](#assertion) is generated. However, it differs from other [client data](#client-data) in some important ways. First, unlike the [client data](#client-data), the [RP ID](#rp-id) of a credential does not change between operations but instead remains the same for the lifetime of that credential. Secondly, it is validated by the authenticator during the [authenticatorGetAssertion](#authenticatorgetassertion) operation, by verifying that the [RP ID](#rp-id) that the requested [credential](#public-key-credential) is [scoped](#scope) to exactly matches the [RP ID](#rp-id) supplied by the [client](#client).

[Authenticators](#authenticator) perform the following steps to generate an [authenticator data](#authenticator-data) structure:

- Hash [RP ID](#rp-id) using SHA-256 to generate the [rpIdHash](#authdata-rpidhash).
- The [UP](#authdata-flags-up) [flag](#authdata-flags) SHALL be set if and only if the authenticator performed a [test of user presence](#test-of-user-presence). The [UV](#authdata-flags-uv) [flag](#authdata-flags) SHALL be set if and only if the authenticator performed [user verification](#user-verification). The `RFU` bits SHALL be set to zero.
	Note: If the authenticator performed both a [test of user presence](#test-of-user-presence) and [user verification](#user-verification), possibly combined in a single [authorization gesture](#authorization-gesture), then the authenticator will set both the [UP](#authdata-flags-up) [flag](#authdata-flags) and the [UV](#authdata-flags-uv) [flag](#authdata-flags).
- The [BE](#authdata-flags-be) [flag](#authdata-flags) SHALL be set if and only if the credential is a [multi-device credential](#multi-device-credential). This value MUST NOT change after a [registration ceremony](#registration-ceremony) as defined in [§ 6.1.3 Credential Backup State](#sctn-credential-backup).
- The [BS](#authdata-flags-bs) [flag](#authdata-flags) SHALL be set if and only if the credential is a [multi-device credential](#multi-device-credential) and is currently [backed up](#backed-up).
	If the backup status of a credential is uncertain or the authenticator suspects a problem with the backed up credential, the [BS](#authdata-flags-bs) [flag](#authdata-flags) SHOULD NOT be set.
- For [attestation signatures](#attestation-signature), the authenticator MUST set the [AT](#authdata-flags-at) [flag](#authdata-flags) and include the `attestedCredentialData`. For [assertion signatures](#assertion-signature), the [AT](#authdata-flags-at) [flag](#authdata-flags) MUST NOT be set and the `attestedCredentialData` MUST NOT be included.
- If the authenticator does not include any [extension data](#authdata-extensions), it MUST set the [ED](#authdata-flags-ed) [flag](#authdata-flags) to zero, and to one if [extension data](#authdata-extensions) is included.

[Figure](#fig-authData) shows a visual representation of the [authenticator data](#authenticator-data) structure.

![](https://yubicolabs.github.io/webauthn-sign-extension/4/images/fido-signature-formats-figure1.svg)

Authenticator data layout.

Note: The [authenticator data](#authenticator-data) describes its own length: If the [AT](#authdata-flags-at) and [ED](#authdata-flags-ed) [flags](#authdata-flags) are not set, it is always 37 bytes long. The [attested credential data](#attested-credential-data) (which is only present if the [AT](#authdata-flags-at) [flag](#authdata-flags) is set) describes its own length. If the [ED](#authdata-flags-ed) [flag](#authdata-flags) is set, then the total length is 37 bytes plus the length of the [attested credential data](#attested-credential-data) (if the [AT](#authdata-flags-at) [flag](#authdata-flags) is set), plus the length of the [extensions](#authdata-extensions) output (a [CBOR](#cbor) map) that follows.

Determining [attested credential data](#attested-credential-data) ’s length, which is variable, involves determining `credentialPublicKey` ’s beginning location given the preceding `credentialId` ’s [length](#authdata-attestedcredentialdata-credentialidlength), and then determining the `credentialPublicKey` ’s length (see also [Section 7](https://tools.ietf.org/html/rfc9052#section-7) of [\[RFC9052\]](#biblio-rfc9052 "CBOR Object Signing and Encryption (COSE): Structures and Process")).

#### 6.1.1. Signature Counter Considerations

Authenticators SHOULD implement a [signature counter](#signature-counter) feature. These counters are conceptually stored for each credential by the authenticator, or globally for the authenticator as a whole. The initial value of a credential’s [signature counter](#signature-counter) is specified in the `signCount` value of the [authenticator data](#authenticator-data) returned by [authenticatorMakeCredential](#authenticatormakecredential). The [signature counter](#signature-counter) is incremented for each successful [authenticatorGetAssertion](#authenticatorgetassertion) operation by some positive value, and subsequent values are returned to the [WebAuthn Relying Party](#webauthn-relying-party) within the [authenticator data](#authenticator-data) again. The [signature counter](#signature-counter) ’s purpose is to aid [Relying Parties](#relying-party) in detecting cloned authenticators. Clone detection is more important for authenticators with limited protection measures.

Authenticators that do not implement a [signature counter](#signature-counter) leave the `signCount` in the [authenticator data](#authenticator-data) constant at zero.

A [Relying Party](#relying-party) stores the [signature counter](#signature-counter) of the most recent [authenticatorGetAssertion](#authenticatorgetassertion) operation. (Or the counter from the [authenticatorMakeCredential](#authenticatormakecredential) operation if no [authenticatorGetAssertion](#authenticatorgetassertion) has ever been performed on a credential.) In subsequent [authenticatorGetAssertion](#authenticatorgetassertion) operations, the [Relying Party](#relying-party) compares the stored [signature counter](#signature-counter) value with the new `signCount` value returned in the assertion’s [authenticator data](#authenticator-data). If either is non-zero, and the new `signCount` value is less than or equal to the stored value, a cloned authenticator may exist, or the authenticator may be malfunctioning, or a race condition might exist where the relying party is receiving and processing assertions in an order other than the order they were generated at the authenticator.

Detecting a [signature counter](#signature-counter) mismatch does not indicate whether the current operation was performed by a cloned authenticator or the original authenticator. [Relying Parties](#relying-party) should address this situation appropriately relative to their individual situations, i.e., their risk tolerance or operational factors that might result in an acceptable reason for non-increasing values.

Authenticators:

- SHOULD implement per credential [signature counters](#signature-counter). This prevents the [signature counter](#signature-counter) value from being shared between [Relying Parties](#relying-party) and being possibly employed as a correlation handle for the user. Authenticators MAY implement a global [signature counter](#signature-counter), i.e., on a per-authenticator basis, but this is less privacy-friendly for users.
- SHOULD ensure that the [signature counter](#signature-counter) value does not accidentally decrease (e.g., due to hardware failures).

#### 6.1.2. FIDO U2F Signature Format Compatibility

The format for [assertion signatures](#assertion-signature), which sign over the concatenation of an [authenticator data](#authenticator-data) structure and the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data), are compatible with the FIDO U2F authentication signature format (see [Section 5.4](https://fidoalliance.org/specs/fido-u2f-v1.1-id-20160915/fido-u2f-raw-message-formats-v1.1-id-20160915.html#authentication-response-message-success) of [\[FIDO-U2F-Message-Formats\]](#biblio-fido-u2f-message-formats "FIDO U2F Raw Message Formats")).

This is because the first 37 bytes of the signed data in a FIDO U2F authentication response message constitute a valid [authenticator data](#authenticator-data) structure, and the remaining 32 bytes are the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data). In this [authenticator data](#authenticator-data) structure, the `rpIdHash` is the FIDO U2F [application parameter](https://fidoalliance.org/specs/fido-u2f-v1.1-id-20160915/fido-u2f-raw-message-formats-v1.1-id-20160915.html#authentication-request-message---u2f_authenticate), all `flags` except `UP` are always zero, and the `attestedCredentialData` and `extensions` are never present. FIDO U2F authentication signatures can therefore be verified by the same procedure as other [assertion signatures](#assertion-signature) generated by the [authenticatorGetAssertion](#authenticatorgetassertion) operation.

#### 6.1.3. Credential Backup State

Credential [backup eligibility](#backup-eligibility) and current [backup state](#backup-state) is conveyed by the [BE](#authdata-flags-be) and [BS](#authdata-flags-bs) [flags](#authdata-flags) in the [authenticator data](#authenticator-data), as defined in [Table](#table-authData) .

The value of the [BE](#authdata-flags-be) [flag](#authdata-flags) is set during [authenticatorMakeCredential](#authenticatormakecredential) operation and MUST NOT change.

The value of the [BS](#authdata-flags-bs) [flag](#authdata-flags) may change over time based on the current state of the [public key credential source](#public-key-credential-source). [Table](#table-backupStates) below defines valid combinations and their meaning.

| [BE](#authdata-flags-be) | [BS](#authdata-flags-bs) | Description |
| --- | --- | --- |
| `0` | `0` | The credential is a [single-device credential](#single-device-credential). |
| `0` | `1` | This combination is not allowed. |
| `1` | `0` | The credential is a [multi-device credential](#multi-device-credential) and is not currently [backed up](#backed-up). |
| `1` | `1` | The credential is a [multi-device credential](#multi-device-credential) and is currently [backed up](#backed-up). |

[BE](#authdata-flags-be) and [BS](#authdata-flags-bs) [flag](#authdata-flags) combinations

It is RECOMMENDED that [Relying Parties](#relying-party) store the most recent value of these [flags](#authdata-flags) with the [user account](#user-account) for future evaluation.

The following is a non-exhaustive list of how [Relying Parties](#relying-party) might use these [flags](#authdata-flags):

- Requiring additional [authenticators](#authenticator):
	When the [BE](#authdata-flags-be) [flag](#authdata-flags) is set to `0`, the credential is a [single-device credential](#single-device-credential) and the [generating authenticator](#generating-authenticator) will never allow the credential to be backed up.
	A [single-device credential](#single-device-credential) is not resilient to single device loss. [Relying Parties](#relying-party) SHOULD ensure that each [user account](#user-account) has additional [authenticators](#authenticator) [registered](#registration-ceremony) and/or an account recovery process in place. For example, the user could be prompted to set up an additional [authenticator](#authenticator), such as a [roaming authenticator](#roaming-authenticators) or an [authenticator](#authenticator) that is capable of [multi-device credentials](#multi-device-credential).
- Upgrading a user to a password-free account:
	When the [BS](#authdata-flags-bs) [flag](#authdata-flags) changes from `0` to `1`, the [authenticator](#authenticator) is signaling that the [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) is backed up and is protected from single device loss.
	The [Relying Party](#relying-party) MAY choose to prompt the user to upgrade their account security and remove their password.
- Adding an additional factor after a state change:
	When the [BS](#authdata-flags-bs) [flag](#authdata-flags) changes from `1` to `0`, the [authenticator](#authenticator) is signaling that the [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) is no longer backed up, and no longer protected from single device loss. This could be the result of the user actions, such as disabling the backup service, or errors, such as issues with the backup service.
	When this transition occurs, the [Relying Party](#relying-party) SHOULD guide the user through a process to validate their other authentication factors. If the user does not have another credential for their account, they SHOULD be guided through adding an additional credential to ensure they do not lose access to their account. For example, the user could be prompted to set up an additional [authenticator](#authenticator), such as a [roaming authenticator](#roaming-authenticators) or an [authenticator](#authenticator) that is capable of [multi-device credentials](#multi-device-credential).

### 6.2. Authenticator Taxonomy

Many use cases are dependent on the capabilities of the [authenticator](#authenticator) used. This section defines some terminology for those capabilities, their most important combinations, and which use cases those combinations enable.

For example:

- When authenticating for the first time on a particular [client device](#client-device), a [roaming authenticator](#roaming-authenticators) is typically needed since the user doesn’t yet have a [platform credential](#platform-credential) on that [client device](#client-device).
- For subsequent re-authentication on the same [client device](#client-device), a [platform authenticator](#platform-authenticators) is likely the most convenient since it’s built directly into the [client device](#client-device) rather than being a separate device that the user may have to locate.
- For [second-factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) authentication in addition to a traditional username and password, any [authenticator](#authenticator) can be used.
- Passwordless [multi-factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) authentication requires an [authenticator](#authenticator) capable of [user verification](#user-verification), and in some cases also [discoverable credential capable](#discoverable-credential-capable).
- A laptop computer might support connecting to [roaming authenticators](#roaming-authenticators) via USB and Bluetooth, while a mobile phone might only support NFC.

The above examples illustrate the primary authenticator type characteristics:

- Whether the [authenticator](#authenticator) is a [roaming](#roaming-authenticators) or [platform](#platform-authenticators) authenticator, or in some cases both — the. A [roaming authenticator](#roaming-authenticators) can support one or more [transports](#enum-transport) for communicating with the [client](#client).
- Whether the authenticator is capable of [user verification](#user-verification) — the [authentication factor capability](#authentication-factor-capability).
- Whether the authenticator is [discoverable credential capable](#discoverable-credential-capable) — the.

These characteristics are independent and may in theory be combined in any way, but [Table](#table-authenticatorTypes) lists and names some [authenticator types](#authenticator-type) of particular interest.

| [Authenticator Type](#authenticator-type) |  |  | [Authentication Factor Capability](#authentication-factor-capability) |
| --- | --- | --- | --- |
| Second-factor platform authenticator | [platform](#platform-attachment) | Either | [Single-factor capable](#single-factor-capable) |
| User-verifying platform authenticator | [platform](#platform-attachment) | Either | [Multi-factor capable](#multi-factor-capable) |
| Second-factor roaming authenticator | [cross-platform](#cross-platform-attachment) |  | [Single-factor capable](#single-factor-capable) |
| Passkey roaming authenticator | [cross-platform](#cross-platform-attachment) |  | [Multi-factor capable](#multi-factor-capable) |
| Passkey platform authenticator | [platform](#platform-attachment) (`transport` = `internal`) or [cross-platform](#cross-platform-attachment) (`transport` = `hybrid`) |  | [Multi-factor capable](#multi-factor-capable) |

Definitions of names for some [authenticator types](#authenticator-type).

A [second-factor platform authenticator](#second-factor-platform-authenticator) is convenient to use for re-authentication on the same [client device](#client-device), and can be used to add an extra layer of security both when initiating a new session and when resuming an existing session. A [second-factor roaming authenticator](#second-factor-roaming-authenticator) is more likely to be used to authenticate on a particular [client device](#client-device) for the first time, or on a [client device](#client-device) shared between multiple users.

[Passkey platform authenticators](#passkey-platform-authenticator) and [passkey roaming authenticators](#passkey-roaming-authenticator) enable passwordless [multi-factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) authentication. In addition to the proof of possession of the [credential private key](#credential-private-key), these authenticators support [user verification](#user-verification) as a second [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af), typically a PIN or [biometric recognition](#biometric-recognition). The [authenticator](#authenticator) can thus act as two kinds of [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af), which enables [multi-factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) authentication while eliminating the need to share a password with the [Relying Party](#relying-party). These authenticators also support [discoverable credentials](#discoverable-credential), also called [passkeys](#passkey), meaning they also enable authentication flows where username input is not necessary.

The [user-verifying platform authenticator](#user-verifying-platform-authenticator) class is largely obsoleted by the [passkey platform authenticator](#passkey-platform-authenticator) class, but the definition is still used by the `isUserVerifyingPlatformAuthenticatorAvailable` method.

The combinations not named in [Table](#table-authenticatorTypes) have less distinguished use cases:

- A [roaming authenticator](#roaming-authenticators) that is [discoverable credential capable](#discoverable-credential-capable) but not [multi-factor capable](#multi-factor-capable) can be used for [single-factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#sf) authentication without a username, where the user is automatically identified by the [user handle](#user-handle) and possession of the [credential private key](#credential-private-key) is used as the only [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af). This can be useful in some situations, but makes the user particularly vulnerable to theft of the [authenticator](#authenticator).
- A [roaming authenticator](#roaming-authenticators) that is [multi-factor capable](#multi-factor-capable) but not [discoverable credential capable](#discoverable-credential-capable) can be used for [multi-factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) authentication, but requires the user to be identified first which risks leaking personally identifying information; see [§ 14.6.3 Privacy leak via credential IDs](#sctn-credential-id-privacy-leak).

The following subsections define the aspects, and [authentication factor capability](#authentication-factor-capability) in more depth.

#### 6.2.1.

[Clients](#client) can communicate with [authenticators](#authenticator) using a variety of mechanisms. For example, a [client](#client) MAY use a [client device](#client-device) -specific API to communicate with an [authenticator](#authenticator) which is physically bound to a [client device](#client-device). On the other hand, a [client](#client) can use a variety of standardized cross-platform transport protocols such as Bluetooth (see [§ 5.8.4 Authenticator Transport Enumeration (enum AuthenticatorTransport)](#enum-transport)) to discover and communicate with [cross-platform attached](#cross-platform-attachment) [authenticators](#authenticator). We refer to [authenticators](#authenticator) that are part of the [client device](#client-device) as platform authenticators, while those that are reachable via cross-platform transport protocols are referred to as roaming authenticators.

- A [platform authenticator](#platform-authenticators) is attached using a [client device](#client-device) -specific transport, called platform attachment, and is usually not removable from the [client device](#client-device). A [public key credential](#public-key-credential) [bound](#bound-credential) to a [platform authenticator](#platform-authenticators) is called a platform credential.
- A [roaming authenticator](#roaming-authenticators) is attached using cross-platform transports, called cross-platform attachment. Authenticators of this class are removable from, and can "roam" between, [client devices](#client-device). A [public key credential](#public-key-credential) [bound](#bound-credential) to a [roaming authenticator](#roaming-authenticators) is called a roaming credential.

Some [platform authenticators](#platform-authenticators) could possibly also act as [roaming authenticators](#roaming-authenticators) depending on context. For example, a [platform authenticator](#platform-authenticators) integrated into a mobile device could make itself available as a [roaming authenticator](#roaming-authenticators) via Bluetooth. In this case [clients](#client) running on the mobile device would recognise the authenticator as a [platform authenticator](#platform-authenticators), while [clients](#client) running on a different [client device](#client-device) and communicating with the same authenticator via Bluetooth would recognize it as a [roaming authenticator](#roaming-authenticators).

The primary use case for [platform authenticators](#platform-authenticators) is to register a particular [client device](#client-device) as a "trusted device", so the [client device](#client-device) itself acts as a [something you have](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) for future [authentication](#authentication). This gives the user the convenience benefit of not needing a [roaming authenticator](#roaming-authenticators) for future [authentication ceremonies](#authentication-ceremony), e.g., the user will not have to dig around in their pocket for their key fob or phone.

Use cases for [roaming authenticators](#roaming-authenticators) include: [authenticating](#authentication) on a new [client device](#client-device) for the first time, on rarely used [client devices](#client-device), [client devices](#client-device) shared between multiple users, or [client devices](#client-device) that do not include a [platform authenticator](#platform-authenticators); and when policy or preference dictates that the [authenticator](#authenticator) be kept separate from the [client devices](#client-device) it is used with. A [roaming authenticator](#roaming-authenticators) can also be used to hold backup [credentials](#public-key-credential) in case another [authenticator](#authenticator) is lost.

#### 6.2.2. Credential Storage Modality

An [authenticator](#authenticator) can store a [public key credential source](#public-key-credential-source) in one of two ways:

1. In persistent storage embedded in the [authenticator](#authenticator), [client](#client) or [client device](#client-device), e.g., in a secure element. This is a technical requirement for a [client-side discoverable public key credential source](#client-side-discoverable-public-key-credential-source).
2. By encrypting (i.e., wrapping) the [public key credential source](#public-key-credential-source) such that only this [authenticator](#authenticator) can decrypt (i.e., unwrap) it and letting the resulting ciphertext be the [credential ID](#credential-id) of the [public key credential source](#public-key-credential-source). The [credential ID](#credential-id) is stored by the [Relying Party](#relying-party) and returned to the [authenticator](#authenticator) via the `allowCredentials` option of `get()`, which allows the [authenticator](#authenticator) to decrypt and use the [public key credential source](#public-key-credential-source).
	This enables the [authenticator](#authenticator) to have unlimited credential storage capacity, since the encrypted [public key credential sources](#public-key-credential-source) are stored by the [Relying Party](#relying-party) instead of by the [authenticator](#authenticator) - but it means that a [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) stored in this way must be retrieved from the [Relying Party](#relying-party) before the [authenticator](#authenticator) can use it.

Which of these storage strategies an [authenticator](#authenticator) supports defines the [authenticator](#authenticator) ’s as follows:

- An [authenticator](#authenticator) has the if it supports [client-side discoverable public key credential sources](#client-side-discoverable-public-key-credential-source). An [authenticator](#authenticator) with is also called discoverable credential capable.
- An [authenticator](#authenticator) has the if it does not have the, i.e., it only supports storing [public key credential sources](#public-key-credential-source) as a ciphertext in the [credential ID](#credential-id).

Note that a [discoverable credential capable](#discoverable-credential-capable) [authenticator](#authenticator) MAY support both storage strategies. In this case, the [authenticator](#authenticator) MAY at its discretion use different storage strategies for different [credentials](#public-key-credential), though subject to the `residentKey` and `requireResidentKey` options of `create()`.

#### 6.2.3. Authentication Factor Capability

There are three broad classes of [authentication factors](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) that can be used to prove an identity during an [authentication ceremony](#authentication-ceremony): [something you have](https://pages.nist.gov/800-63-3/sp800-63-3.html#af), [something you know](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) and [something you are](https://pages.nist.gov/800-63-3/sp800-63-3.html#af). Examples include a physical key, a password, and a fingerprint, respectively.

All [WebAuthn Authenticators](#webauthn-authenticator) belong to the [something you have](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) class, but an [authenticator](#authenticator) that supports [user verification](#user-verification) can also act as one or two additional kinds of [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af). For example, if the [authenticator](#authenticator) can verify a PIN, the PIN is [something you know](https://pages.nist.gov/800-63-3/sp800-63-3.html#af), and a [biometric authenticator](#biometric-authenticator) can verify [something you are](https://pages.nist.gov/800-63-3/sp800-63-3.html#af). Therefore, an [authenticator](#authenticator) that supports [user verification](#user-verification) is multi-factor capable. Conversely, an [authenticator](#authenticator) that is not [multi-factor capable](#multi-factor-capable) is single-factor capable. Note that a single [multi-factor capable](#multi-factor-capable) [authenticator](#authenticator) could support several modes of [user verification](#user-verification), meaning it could act as all three kinds of [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af).

Although [user verification](#user-verification) is performed locally on the [authenticator](#authenticator) and not by the [Relying Party](#relying-party), the [authenticator](#authenticator) indicates if [user verification](#user-verification) was performed by setting the [UV](#authdata-flags-uv) [flag](#authdata-flags) in the signed response returned to the [Relying Party](#relying-party). The [Relying Party](#relying-party) can therefore use the [UV](#authdata-flags-uv) [flag](#authdata-flags) to verify that additional [authentication factors](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) were used in a [registration](#registration) or [authentication ceremony](#authentication-ceremony). The authenticity of the [UV](#authdata-flags-uv) [flag](#authdata-flags) can in turn be assessed by inspecting the [authenticator](#authenticator) ’s [attestation statement](#attestation-statement).

### 6.3. Authenticator Operations

A [WebAuthn Client](#webauthn-client) MUST connect to an authenticator in order to invoke any of the operations of that authenticator. This connection defines an authenticator session. An authenticator must maintain isolation between sessions. It may do this by only allowing one session to exist at any particular time, or by providing more complicated session management.

The following operations can be invoked by the client in an authenticator session.

#### 6.3.1. Lookup Credential Source by Credential ID Algorithm

The result of looking up a [credential id](#credential-id) credentialId in an [authenticator](#authenticator) authenticator is the result of the following algorithm:

1. If authenticator can decrypt credentialId into a [public key credential source](#public-key-credential-source) credSource:
	1. Set credSource.[id](#public-key-credential-source-id) to credentialId.
		2. Return credSource.
2. [For each](https://infra.spec.whatwg.org/#map-iterate) [public key credential source](#public-key-credential-source) credSource of authenticator ’s [credentials map](#authenticator-credentials-map):
	1. If credSource.[id](#public-key-credential-source-id) is credentialId, return credSource.
3. Return `null`.

#### 6.3.2. The authenticatorMakeCredential Operation

Before invoking this operation, the client MUST invoke the [authenticatorCancel](#authenticatorcancel) operation in order to abort all other operations in progress in the [authenticator session](#authenticator-session).

This operation takes the following input parameters:

hash

The [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data), provided by the client.

rpEntity

The [Relying Party](#relying-party) ’s `PublicKeyCredentialRpEntity`.

userEntity

The [user account’s](#user-account) `PublicKeyCredentialUserEntity`, containing the [user handle](#user-handle) given by the [Relying Party](#relying-party).

requireResidentKey

The [effective resident key requirement for credential creation](#effective-resident-key-requirement-for-credential-creation), a Boolean value determined by the [client](#client).

requireUserPresence

The constant Boolean value `true`, or FALSE when `` options.`mediation` `` is set to `conditional` and the user agent previously collected consent from the user.

requireUserVerification

The [effective user verification requirement for credential creation](#effective-user-verification-requirement-for-credential-creation), a Boolean value determined by the [client](#client).

credTypesAndPubKeyAlgs

A sequence of pairs of `PublicKeyCredentialType` and public key algorithms (`COSEAlgorithmIdentifier`) requested by the [Relying Party](#relying-party). This sequence is ordered from most preferred to least preferred. The [authenticator](#authenticator) makes a best-effort to create the most preferred credential that it can.

excludeCredentialDescriptorList

An OPTIONAL list of `PublicKeyCredentialDescriptor` objects provided by the [Relying Party](#relying-party) with the intention that, if any of these are known to the authenticator, it SHOULD NOT create a new credential. excludeCredentialDescriptorList contains a list of known credentials.

enterpriseAttestationPossible

A Boolean value that indicates that individually-identifying attestation MAY be returned by the authenticator.

attestationFormats

A sequence of strings that expresses the [Relying Party](#relying-party) ’s preference for attestation statement formats, from most to least preferable. If the [authenticator](#authenticator) returns [attestation](#attestation), then it makes a best-effort attempt to use the most preferable format that it supports.

extensions

A [CBOR](#cbor) [map](https://infra.spec.whatwg.org/#ordered-map) from [extension identifiers](#extension-identifier) to their [authenticator extension inputs](#authenticator-extension-input), created by the [client](#client) based on the extensions requested by the [Relying Party](#relying-party), if any.

When this operation is invoked, the [authenticator](#authenticator) MUST perform the following procedure:

1. Check if all the supplied parameters are syntactically well-formed and of the correct length. If not, return an error code equivalent to " `UnknownError` " and terminate the operation.
2. Check if at least one of the specified combinations of `PublicKeyCredentialType` and cryptographic parameters in credTypesAndPubKeyAlgs is supported. If not, return an error code equivalent to " `NotSupportedError` " and terminate the operation.
3. [For each](https://infra.spec.whatwg.org/#list-iterate) descriptor of excludeCredentialDescriptorList:
	1. If [looking up](#credential-id-looking-up) `` descriptor.`id` `` in this authenticator returns non-null, and the returned [item](https://infra.spec.whatwg.org/#list-item) ’s [RP ID](#rp-id) and [type](#public-key-credential-source-type) match `` rpEntity.`id` `` and `` excludeCredentialDescriptorList.`type` `` respectively, then collect an [authorization gesture](#authorization-gesture) confirming for creating a new credential. The [authorization gesture](#authorization-gesture) MUST include a [test of user presence](#test-of-user-presence). If the user
		confirms consent to create a new credential
		return an error code equivalent to " `InvalidStateError` " and terminate the operation.
		does not consent to create a new credential
		return an error code equivalent to " `NotAllowedError` " and terminate the operation.
		Note: The purpose of this [authorization gesture](#authorization-gesture) is not to proceed with creating a credential, but for privacy reasons to authorize disclosure of the fact that `` descriptor.`id` `` is [bound](#bound-credential) to this [authenticator](#authenticator). If the user consents, the [client](#client) and [Relying Party](#relying-party) can detect this and guide the user to use a different [authenticator](#authenticator). If the user does not consent, the [authenticator](#authenticator) does not reveal that `` descriptor.`id` `` is [bound](#bound-credential) to it, and responds as if the user simply declined consent to create a credential.
4. If requireResidentKey is `true` and the authenticator cannot store a [client-side discoverable public key credential source](#client-side-discoverable-public-key-credential-source), return an error code equivalent to " `ConstraintError` " and terminate the operation.
5. If requireUserVerification is `true` and the authenticator cannot perform [user verification](#user-verification), return an error code equivalent to " `ConstraintError` " and terminate the operation.
6. Once the [authorization gesture](#authorization-gesture) has been completed and has been obtained, generate a new credential object:
	1. Let (publicKey, privateKey) be a new pair of cryptographic keys using the combination of `PublicKeyCredentialType` and cryptographic parameters represented by the first [item](https://infra.spec.whatwg.org/#list-item) in credTypesAndPubKeyAlgs that is supported by this authenticator.
		2. Let userHandle be `` userEntity.`id` ``.
		3. Let credentialSource be a new [public key credential source](#public-key-credential-source) with the fields:
		[type](#public-key-credential-source-type)
		`public-key`.
		[privateKey](#public-key-credential-source-privatekey)
		privateKey
		[rpId](#public-key-credential-source-rpid)
		`` rpEntity.`id` ``
		[userHandle](#public-key-credential-source-userhandle)
		userHandle
		[otherUI](#public-key-credential-source-otherui)
		Any other information the authenticator chooses to include.
		4. If requireResidentKey is `true` or the authenticator chooses to create a [client-side discoverable public key credential source](#client-side-discoverable-public-key-credential-source):
		1. Let credentialId be a new [credential id](#credential-id).
				2. Set credentialSource.[id](#public-key-credential-source-id) to credentialId.
				3. Let credentials be this authenticator’s [credentials map](#authenticator-credentials-map).
				4. [Set](https://infra.spec.whatwg.org/#map-set) credentials \[(`` rpEntity.`id` ``, userHandle)\] to credentialSource.
		5. Otherwise:
		1. Let credentialId be the result of serializing and encrypting credentialSource so that only this authenticator can decrypt it.
7. If any error occurred while creating the new credential object, return an error code equivalent to " `UnknownError` " and terminate the operation.
8. Let processedExtensions be the result of [authenticator extension processing](#authenticator-extension-processing) [for each](https://infra.spec.whatwg.org/#map-iterate) supported [extension identifier](#extension-identifier) → [authenticator extension input](#authenticator-extension-input) in extensions.
9. If the [authenticator](#authenticator):
	is a U2F device
	let the [signature counter](#signature-counter) value for the new credential be zero. (U2F devices may support signature counters but do not return a counter when making a credential. See [\[FIDO-U2F-Message-Formats\]](#biblio-fido-u2f-message-formats "FIDO U2F Raw Message Formats").)
	supports a global [signature counter](#signature-counter)
	Use the global [signature counter](#signature-counter) ’s actual value when generating [authenticator data](#authenticator-data).
	supports a per credential [signature counter](#signature-counter)
	allocate the counter, associate it with the new credential, and initialize the counter value as zero.
	does not support a [signature counter](#signature-counter)
	let the [signature counter](#signature-counter) value for the new credential be constant at zero.
10. Let attestedCredentialData be the [attested credential data](#attested-credential-data) byte array including the credentialId and publicKey.
11. Let attestationFormat be the first supported [attestation statement format identifier](#attestation-statement-format-identifier) from attestationFormats, taking into account enterpriseAttestationPossible. If attestationFormats contains no supported value, then let attestationFormat be the [attestation statement format identifier](#attestation-statement-format-identifier) most preferred by this authenticator.
12. Let authenticatorData [be the byte array](#authenticator-data-perform-the-following-steps-to-generate-an-authenticator-data-structure) specified in [§ 6.1 Authenticator Data](#sctn-authenticator-data), including attestedCredentialData as the `attestedCredentialData` and processedExtensions, if any, as the `extensions`.
13. Create an [attestation object](#attestation-object) for the new credential using the procedure specified in [§ 6.5.4 Generating an Attestation Object](#sctn-generating-an-attestation-object), the [attestation statement format](#attestation-statement-format) attestationFormat, and the values authenticatorData and hash, as well as `taking into account` the value of enterpriseAttestationPossible. For more details on attestation, see [§ 6.5 Attestation](#sctn-attestation).

On successful completion of this operation, the authenticator returns the [attestation object](#attestation-object) to the client.

#### 6.3.3. The authenticatorGetAssertion Operation

Before invoking this operation, the client MUST invoke the [authenticatorCancel](#authenticatorcancel) operation in order to abort all other operations in progress in the [authenticator session](#authenticator-session).

This operation takes the following input parameters:

rpId

The caller’s [RP ID](#rp-id), as [determined](#GetAssn-DetermineRpId) by the user agent and the client.

hash

The [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data), provided by the client.

allowCredentialDescriptorList

An OPTIONAL [list](https://infra.spec.whatwg.org/#list) of `PublicKeyCredentialDescriptor` s describing credentials acceptable to the [Relying Party](#relying-party) (possibly filtered by the client), if any.

requireUserPresence

The constant Boolean value `true`. It is included here as a pseudo-parameter to simplify applying this abstract authenticator model to implementations that may wish to make a [test of user presence](#test-of-user-presence) optional although WebAuthn does not.

requireUserVerification

The [effective user verification requirement for assertion](#effective-user-verification-requirement-for-assertion), a Boolean value provided by the client.

extensions

A [CBOR](#cbor) [map](https://infra.spec.whatwg.org/#ordered-map) from [extension identifiers](#extension-identifier) to their [authenticator extension inputs](#authenticator-extension-input), created by the client based on the extensions requested by the [Relying Party](#relying-party), if any.

When this method is invoked, the [authenticator](#authenticator) MUST perform the following procedure:

1. Check if all the supplied parameters are syntactically well-formed and of the correct length. If not, return an error code equivalent to " `UnknownError` " and terminate the operation.
2. Let credentialOptions be a new empty [set](https://infra.spec.whatwg.org/#ordered-set) of [public key credential sources](#public-key-credential-source).
3. If allowCredentialDescriptorList was supplied, then [for each](https://infra.spec.whatwg.org/#list-iterate) descriptor of allowCredentialDescriptorList:
	1. Let credSource be the result of [looking up](#credential-id-looking-up) `` descriptor.`id` `` in this authenticator.
		2. If credSource is not `null`, [append](https://infra.spec.whatwg.org/#set-append) it to credentialOptions.
4. Otherwise (allowCredentialDescriptorList was not supplied), [for each](https://infra.spec.whatwg.org/#map-iterate) key → credSource of this authenticator’s [credentials map](#authenticator-credentials-map), [append](https://infra.spec.whatwg.org/#set-append) credSource to credentialOptions.
5. [Remove](https://infra.spec.whatwg.org/#list-remove) any items from credentialOptions whose [rpId](#public-key-credential-source-rpid) is not equal to rpId.
6. If credentialOptions is now empty, return an error code equivalent to " `NotAllowedError` " and terminate the operation.
7. Prompt the user to select a [public key credential source](#public-key-credential-source) selectedCredential from credentialOptions. Collect an [authorization gesture](#authorization-gesture) confirming for using selectedCredential. The prompt for the [authorization gesture](#authorization-gesture) may be shown by the [authenticator](#authenticator) if it has its own output capability, or by the user agent otherwise.
	If requireUserVerification is `true`, the [authorization gesture](#authorization-gesture) MUST include [user verification](#user-verification).
	If requireUserPresence is `true`, the [authorization gesture](#authorization-gesture) MUST include a [test of user presence](#test-of-user-presence).
	If the user does not, return an error code equivalent to " `NotAllowedError` " and terminate the operation.
8. Let processedExtensions be the result of [authenticator extension processing](#authenticator-extension-processing) [for each](https://infra.spec.whatwg.org/#map-iterate) supported [extension identifier](#extension-identifier) → [authenticator extension input](#authenticator-extension-input) in extensions.
9. Increment the credential associated [signature counter](#signature-counter) or the global [signature counter](#signature-counter) value, depending on which approach is implemented by the [authenticator](#authenticator), by some positive value. If the [authenticator](#authenticator) does not implement a [signature counter](#signature-counter), let the [signature counter](#signature-counter) value remain constant at zero.
10. Let authenticatorData [be the byte array](#authenticator-data-perform-the-following-steps-to-generate-an-authenticator-data-structure) specified in [§ 6.1 Authenticator Data](#sctn-authenticator-data) including processedExtensions, if any, as the `extensions` and excluding `attestedCredentialData`.
11. Let signature be the [assertion signature](#assertion-signature) of the concatenation `authenticatorData || hash` using the [privateKey](#public-key-credential-source-privatekey) of selectedCredential as shown in [Figure](#fig-signature) , below. A simple, undelimited concatenation is safe to use here because the [authenticator data](#authenticator-data) describes its own length. The [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data) (which potentially has a variable length) is always the last element.
	![](https://yubicolabs.github.io/webauthn-sign-extension/4/images/fido-signature-formats-figure2.svg)
	Generating an assertion signature.
12. If any error occurred while generating the [assertion signature](#assertion-signature), return an error code equivalent to " `UnknownError` " and terminate the operation.
13. Return to the user agent:
	- selectedCredential.[id](#public-key-credential-source-id), if either a list of credentials (i.e., allowCredentialDescriptorList) of length 2 or greater was supplied by the client, or no such list was supplied.
		Note: If, within allowCredentialDescriptorList, the client supplied exactly one credential and it was successfully employed, then its [credential ID](#credential-id) is not returned since the client already knows it. This saves transmitting these bytes over what may be a constrained connection in what is likely a common case.
		- authenticatorData
		- signature
		- selectedCredential.[userHandle](#public-key-credential-source-userhandle)
		Note: In cases where allowCredentialDescriptorList was supplied the returned [userHandle](#public-key-credential-source-userhandle) value may be `null`, see: [userHandleResult](#assertioncreationdata-userhandleresult).

If the [authenticator](#authenticator) cannot find any [credential](#public-key-credential) corresponding to the specified [Relying Party](#relying-party) that matches the specified criteria, it terminates the operation and returns an error.

#### 6.3.4. The authenticatorCancel Operation

This operation takes no input parameters and returns no result.

When this operation is invoked by the client in an [authenticator session](#authenticator-session), it has the effect of terminating any [authenticatorMakeCredential](#authenticatormakecredential) or [authenticatorGetAssertion](#authenticatorgetassertion) operation currently in progress in that authenticator session. The authenticator stops prompting for, or accepting, any user input related to authorizing the canceled operation. The client ignores any further responses from the authenticator for the canceled operation.

This operation is ignored if it is invoked in an [authenticator session](#authenticator-session) which does not have an [authenticatorMakeCredential](#authenticatormakecredential) or [authenticatorGetAssertion](#authenticatorgetassertion) operation currently in progress.

#### 6.3.5. The silentCredentialDiscovery operation

This is an OPTIONAL operation authenticators MAY support to enable `conditional` [user mediation](https://w3c.github.io/webappsec-credential-management/#user-mediated).

It takes the following input parameter:

rpId

The caller’s [RP ID](#rp-id), as [determined](#GetAssn-DetermineRpId) by the [client](#client).

When this operation is invoked, the [authenticator](#authenticator) MUST perform the following procedure:

1. Let collectedDiscoverableCredentialMetadata be a new [list](https://infra.spec.whatwg.org/#list) whose [items](https://infra.spec.whatwg.org/#list-item) are [structs](https://infra.spec.whatwg.org/#struct) with the following [items](https://infra.spec.whatwg.org/#struct-item):
	A `PublicKeyCredentialType`.
	A [Credential ID](#credential-id).
	rpId
	A [Relying Party Identifier](#relying-party-identifier).
	userHandle
	A [user handle](#user-handle).
	Other information used by the [authenticator](#authenticator) to inform its UI.
2. [For each](https://infra.spec.whatwg.org/#map-iterate) [public key credential source](#public-key-credential-source) credSource of authenticator ’s [credentials map](#authenticator-credentials-map):
	1. If credSource is not a [client-side discoverable credential](#client-side-discoverable-credential), [continue](https://infra.spec.whatwg.org/#iteration-continue).
		2. If credSource.[rpId](#public-key-credential-source-rpid) is not rpId, [continue](https://infra.spec.whatwg.org/#iteration-continue).
		3. Let discoveredCredentialMetadata be a new [struct](https://infra.spec.whatwg.org/#struct) whose [items](https://infra.spec.whatwg.org/#struct-item) are copies of credSource ’s [type](#public-key-credential-source-type), [id](#public-key-credential-source-id), [rpId](#public-key-credential-source-rpid), [userHandle](#public-key-credential-source-userhandle) and [otherUI](#public-key-credential-source-otherui).
		4. [Append](https://infra.spec.whatwg.org/#list-append) discoveredCredentialMetadata to collectedDiscoverableCredentialMetadata.
3. Return collectedDiscoverableCredentialMetadata.

### 6.4. String Handling

Authenticators may be required to store arbitrary strings chosen by a [Relying Party](#relying-party), for example the `name` and `displayName` in a `PublicKeyCredentialUserEntity`. This section discusses some practical consequences of handling arbitrary strings that may be presented to humans.

#### 6.4.1. String Truncation

Each arbitrary string in the API will have some accommodation for the potentially limited resources available to an [authenticator](#authenticator). When the chosen accommodation is string truncation, care needs to be taken to not corrupt the string value.

For example, truncation based on Unicode code points alone may cause a [grapheme cluster](https://w3c.github.io/i18n-glossary/#dfn-grapheme-cluster) to be truncated. This could make the grapheme cluster render as a different glyph, potentially changing the meaning of the string, instead of removing the glyph entirely. For example, [figure](#fig-stringTruncation) shows the end of a UTF-8 encoded string whose encoding is 65 bytes long. If truncating to 64 bytes then the final 0x88 byte is removed first to satisfy the size limit. Since that leaves a partial UTF-8 code point, the remainder of that code point must also be removed. Since that leaves a partial [grapheme cluster](https://w3c.github.io/i18n-glossary/#dfn-grapheme-cluster), the remainder of that cluster should also be removed.

![](https://yubicolabs.github.io/webauthn-sign-extension/4/images/string-truncation.svg)

The end of a UTF-8 encoded string showing the positions of different truncation boundaries.

The responsibility for handling these concerns falls primarily on the [client](#client), to avoid burdening [authenticators](#authenticator) with understanding character encodings and Unicode character properties. The following subsections define requirements for how clients and authenticators, respectively, may perform string truncation.

##### 6.4.1.1. String Truncation by Clients

When a [WebAuthn Client](#webauthn-client) truncates a string, the truncation behaviour observable by the [Relying Party](#relying-party) MUST satisfy the following requirements:

Choose a size limit equal to or greater than the specified minimum supported length. The string MAY be truncated so that its length in bytes in the UTF-8 character encoding satisfies that limit. This truncation MUST respect UTF-8 code point boundaries, and SHOULD respect [grapheme cluster](https://w3c.github.io/i18n-glossary/#dfn-grapheme-cluster) boundaries [\[UAX29\]](#biblio-uax29 "UNICODE Text Segmentation"). The resulting truncated value MAY be shorter than the chosen size limit but MUST NOT be shorter than the longest prefix substring that satisfies the size limit and ends on a [grapheme cluster](https://w3c.github.io/i18n-glossary/#dfn-grapheme-cluster) boundary.

The client MAY let the [authenticator](#authenticator) perform the truncation if it satisfies these requirements; otherwise the client MUST perform the truncation before relaying the string value to the authenticator.

In addition to the above, truncating on byte boundaries alone causes a known issue that user agents should be aware of: if the authenticator is using [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") then future messages from the authenticator may contain invalid CBOR since the value is typed as a CBOR string and thus is required to be valid UTF-8. Thus, when dealing with [authenticators](#authenticator), user agents SHOULD:

1. Ensure that any strings sent to authenticators are validly encoded.
2. Handle the case where strings have been truncated resulting in an invalid encoding. For example, any partial code point at the end may be dropped or replaced with [U+FFFD](http://unicode.org/cldr/utility/character.jsp?a=FFFD).

##### 6.4.1.2. String Truncation by Authenticators

Because a [WebAuthn Authenticator](#webauthn-authenticator) may be implemented in a constrained environment, the requirements on authenticators are relaxed compared to those for [clients](#client).

When a [WebAuthn Authenticator](#webauthn-authenticator) truncates a string, the truncation behaviour MUST satisfy the following requirements:

Choose a size limit equal to or greater than the specified minimum supported length. The string MAY be truncated so that its length in bytes in the UTF-8 character encoding satisfies that limit. This truncation SHOULD respect UTF-8 code point boundaries, and MAY respect [grapheme cluster](https://w3c.github.io/i18n-glossary/#dfn-grapheme-cluster) boundaries [\[UAX29\]](#biblio-uax29 "UNICODE Text Segmentation"). The resulting truncated value MAY be shorter than the chosen size limit but MUST NOT be shorter than the longest prefix substring that satisfies the size limit and ends on a [grapheme cluster](https://w3c.github.io/i18n-glossary/#dfn-grapheme-cluster) boundary.

#### 6.4.2. Language and Direction Encoding

In order to be correctly displayed in context, the language and base direction of a string [may be required](https://www.w3.org/TR/string-meta/#why-is-this-important). Strings in this API may have to be written to fixed-function [authenticators](#authenticator) and then later read back and displayed on a different platform.

For compatibility with existing fixed-function [authenticators](#authenticator) without support for dedicated language and direction metadata fields, Web Authentication Level 2 included provisions for embedding such metadata in the string itself to ensure that it is transported atomically. This encoding is NOT RECOMMENDED; [clients](#client) and [authenticators](#authenticator) MAY ignore such encoding in new values. [Clients](#client) and [authenticators](#authenticator) MAY detect and process language and direction metadata encoded in existing strings as described in [Web Authentication Level 2 §6.4.2. Language and Direction Encoding](https://www.w3.org/TR/2021/REC-webauthn-2-20210408/#sctn-strings-langdir).

Instead, a future version of the [Web Authentication API](#web-authentication-api) may provide dedicated language and direction metadata fields.

### 6.5. Attestation

[Authenticators](#authenticator) SHOULD also provide some form of [attestation](#attestation), if possible. If an authenticator does, the basic requirement is that the [authenticator](#authenticator) can produce, for each [credential public key](#credential-public-key), an [attestation statement](#attestation-statement) verifiable by the [WebAuthn Relying Party](#webauthn-relying-party). Typically, this [attestation statement](#attestation-statement) contains a signature by an [attestation private key](#attestation-private-key) over the attested [credential public key](#credential-public-key) and a challenge, as well as a certificate or similar data providing provenance information for the [attestation public key](#attestation-public-key), enabling the [Relying Party](#relying-party) to make a trust decision. However, if an [attestation key pair](#attestation-key-pair) is not available, then the authenticator MAY either perform [self attestation](#self-attestation) of the [credential public key](#credential-public-key) with the corresponding [credential private key](#credential-private-key), or otherwise perform [no attestation](#none). All this information is returned by [authenticators](#authenticator) any time a new [public key credential](#public-key-credential) is generated, in the overall form of an attestation object. The relationship of the [attestation object](#attestation-object) with [authenticator data](#authenticator-data) (containing [attested credential data](#attested-credential-data)) and the [attestation statement](#attestation-statement) is illustrated in [figure](#fig-attStructs) , below.

If an [authenticator](#authenticator) employs [self attestation](#self-attestation) or [no attestation](#none), then no provenance information is provided for the [Relying Party](#relying-party) to base a trust decision on. In these cases, the [authenticator](#authenticator) provides no guarantees about its operation to the [Relying Party](#relying-party).

![](https://yubicolabs.github.io/webauthn-sign-extension/4/images/fido-attestation-structures.svg)

Attestation object layout illustrating the included authenticator data (containing attested credential data ) and the attestation statement.

Note: This figure illustrates only the `packed` [attestation statement format](#attestation-statement-format). Several additional [attestation statement formats](#attestation-statement-format) are defined in [§ 8 Defined Attestation Statement Formats](#sctn-defined-attestation-formats).

An important component of the [attestation object](#attestation-object) is the attestation statement. This is a specific type of signed data object, containing statements about a [public key credential](#public-key-credential) itself and the [authenticator](#authenticator) that created it. It contains an [attestation signature](#attestation-signature) created using the key of the attesting authority (except for the case of [self attestation](#self-attestation), when it is created using the [credential private key](#credential-private-key)). In order to correctly interpret an [attestation statement](#attestation-statement), a [Relying Party](#relying-party) needs to understand these two aspects of [attestation](#attestation):

1. The attestation statement format is the manner in which the signature is represented and the various contextual bindings are incorporated into the attestation statement by the [authenticator](#authenticator). In other words, this defines the syntax of the statement. Various existing components and OS platforms (such as TPMs and the Android OS) have previously defined [attestation statement formats](#attestation-statement-format). This specification supports a variety of such formats in an extensible way, as defined in [§ 6.5.2 Attestation Statement Formats](#sctn-attestation-formats). The formats themselves are identified by strings, as described in [§ 8.1 Attestation Statement Format Identifiers](#sctn-attstn-fmt-ids).
2. The attestation type defines the semantics of [attestation statements](#attestation-statement) and their underlying trust models. Specifically, it defines how a [Relying Party](#relying-party) establishes trust in a particular [attestation statement](#attestation-statement), after verifying that it is cryptographically valid. This specification supports a number of [attestation types](#attestation-type), as described in [§ 6.5.3 Attestation Types](#sctn-attestation-types).

In general, there is no simple mapping between [attestation statement formats](#attestation-statement-format) and [attestation types](#attestation-type). For example, the "packed" [attestation statement format](#attestation-statement-format) defined in [§ 8.2 Packed Attestation Statement Format](#sctn-packed-attestation) can be used in conjunction with all [attestation types](#attestation-type), while other formats and types have more limited applicability.

The privacy, security and operational characteristics of [attestation](#attestation) depend on:

- The [attestation type](#attestation-type), which determines the trust model,
- The [attestation statement format](#attestation-statement-format), which MAY constrain the strength of the [attestation](#attestation) by limiting what can be expressed in an [attestation statement](#attestation-statement), and
- The characteristics of the individual [authenticator](#authenticator), such as its construction, whether part or all of it runs in a secure operating environment, and so on.

The [attestation type](#attestation-type) and [attestation statement format](#attestation-statement-format) is chosen by the [authenticator](#authenticator); [Relying Parties](#relying-party) can only signal their preferences by setting the `attestation` and `attestationFormats` parameters.

It is expected that most [authenticators](#authenticator) will support a small number of [attestation types](#attestation-type) and [attestation statement formats](#attestation-statement-format), while [Relying Parties](#relying-party) will decide what [attestation types](#attestation-type) are acceptable to them by policy. [Relying Parties](#relying-party) will also need to understand the characteristics of the [authenticators](#authenticator) that they trust, based on information they have about these [authenticators](#authenticator). For example, the FIDO Metadata Service [\[FIDOMetadataService\]](#biblio-fidometadataservice "FIDO Metadata Service") provides one way to access such information.

#### 6.5.1. Attested Credential Data

Attested credential data is a variable-length byte array added to the [authenticator data](#authenticator-data) when generating an [attestation object](#attestation-object) for a credential. Its format is shown in [Table](#table-attestedCredentialData) .

| Name | Length (in bytes) | Description |
| --- | --- | --- |
| aaguid | 16 | The [AAGUID](#aaguid) of the authenticator. |
| credentialIdLength | 2 | Byte length **L** of [credentialId](#authdata-attestedcredentialdata-credentialid), 16-bit unsigned big-endian integer. Value MUST be ≤ 1023. |
| credentialId | L | [Credential ID](#credential-id) |
| credentialPublicKey | variable | The [credential public key](#credential-public-key) encoded in COSE\_Key format, as defined in [Section 7](https://tools.ietf.org/html/rfc9052#section-7) of [\[RFC9052\]](#biblio-rfc9052 "CBOR Object Signing and Encryption (COSE): Structures and Process"), using the. The COSE\_Key-encoded [credential public key](#credential-public-key) MUST contain the "alg" parameter and MUST NOT contain any other OPTIONAL parameters. The "alg" parameter MUST contain a `COSEAlgorithmIdentifier` value. The encoded [credential public key](#credential-public-key) MUST also contain any additional REQUIRED parameters stipulated by the relevant key type specification, i.e., REQUIRED for the key type "kty" and algorithm "alg" (see [Section 2](https://tools.ietf.org/html/rfc9053#section-2) of [\[RFC9053\]](#biblio-rfc9053 "CBOR Object Signing and Encryption (COSE): Initial Algorithms")). |

[Attested credential data](#attested-credential-data) layout. The names in the Name column are only for reference within this document, and are not present in the actual representation of the [attested credential data](#attested-credential-data).

##### 6.5.1.1. Examples of credentialPublicKey Values Encoded in COSE\_Key Format

This section provides examples of COSE\_Key-encoded Elliptic Curve and RSA public keys for the ES256, PS256, and RS256 signature algorithms. These examples adhere to the rules defined above for the [credentialPublicKey](#authdata-attestedcredentialdata-credentialpublickey) value, and are presented in CDDL [\[RFC8610\]](#biblio-rfc8610 "Concise Data Definition Language (CDDL): A Notational Convention to Express Concise Binary Object Representation (CBOR) and JSON Data Structures") for clarity.

[Section 7](https://tools.ietf.org/html/rfc9052#section-7) of [\[RFC9052\]](#biblio-rfc9052 "CBOR Object Signing and Encryption (COSE): Structures and Process") defines the general framework for all COSE\_Key-encoded keys. Specific key types for specific algorithms are defined in [\[RFC9053\]](#biblio-rfc9053 "CBOR Object Signing and Encryption (COSE): Initial Algorithms") as well as in other specifications, as noted below.

Below is an example of a COSE\_Key-encoded Elliptic Curve public key in EC2 format (see [Section 7.1](https://tools.ietf.org/html/rfc9053#section-7.1) of [\[RFC9053\]](#biblio-rfc9053 "CBOR Object Signing and Encryption (COSE): Initial Algorithms")), on the P-256 curve, to be used with the ES256 signature algorithm (ECDSA w/ SHA-256, see [Section 2.1](https://tools.ietf.org/html/rfc9053#section-2.1) of [\[RFC9053\]](#biblio-rfc9053 "CBOR Object Signing and Encryption (COSE): Initial Algorithms")):

```
{
  1:   2,  ; kty: EC2 key type
  3:  -7,  ; alg: ES256 signature algorithm
 -1:   1,  ; crv: P-256 curve
 -2:   x,  ; x-coordinate as byte string 32 bytes in length
           ; e.g., in hex: 65eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d
 -3:   y   ; y-coordinate as byte string 32 bytes in length
           ; e.g., in hex: 1e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c
}
```

Below is the above Elliptic Curve public key encoded in the, whitespace and line breaks are included here for clarity and to match the CDDL [\[RFC8610\]](#biblio-rfc8610 "Concise Data Definition Language (CDDL): A Notational Convention to Express Concise Binary Object Representation (CBOR) and JSON Data Structures") presentation above:

```
A5
   01  02

   03  26

   20  01

   21  58 20   65eda5a12577c2bae829437fe338701a10aaa375e1bb5b5de108de439c08551d

   22  58 20   1e52ed75701163f7f9e40ddf9f341b3dc9ba860af7e0ca7ca7e9eecd0084d19c
```

Below is an example of a COSE\_Key-encoded 2048-bit RSA public key (see [\[RFC8230\]](#biblio-rfc8230 "Using RSA Algorithms with CBOR Object Signing and Encryption (COSE) Messages") [Section 4](https://tools.ietf.org/html/rfc8230#section-4), to be used with the PS256 signature algorithm (RSASSA-PSS with SHA-256, see [Section 2](https://tools.ietf.org/html/rfc8230#section-2) of [\[RFC8230\]](#biblio-rfc8230 "Using RSA Algorithms with CBOR Object Signing and Encryption (COSE) Messages"):

```
{
  1:   3,  ; kty: RSA key type
  3: -37,  ; alg: PS256
 -1:   n,  ; n:   RSA modulus n byte string 256 bytes in length
           ;      e.g., in hex (middle bytes elided for brevity): DB5F651550...6DC6548ACC3
 -2:   e   ; e:   RSA public exponent e byte string 3 bytes in length
           ;      e.g., in hex: 010001
}
```

Below is an example of the same COSE\_Key-encoded RSA public key as above, to be used with the RS256 signature algorithm (RSASSA-PKCS1-v1\_5 with SHA-256):

```
{
  1:   3,  ; kty: RSA key type
  3:-257,  ; alg: RS256
 -1:   n,  ; n:   RSA modulus n byte string 256 bytes in length
           ;      e.g., in hex (middle bytes elided for brevity): DB5F651550...6DC6548ACC3
 -2:   e   ; e:   RSA public exponent e byte string 3 bytes in length
           ;      e.g., in hex: 010001
}
```

#### 6.5.2. Attestation Statement Formats

As described above, an [attestation statement format](#attestation-statement-format) is a data format which represents a cryptographic signature by an [authenticator](#authenticator) over a set of contextual bindings. Each [attestation statement format](#attestation-statement-format) MUST be defined using the following template:

- **[Attestation statement format identifier](#attestation-statement-format-identifier):**
- **Supported [attestation types](#attestation-type):**
- **Syntax:** The syntax of an [attestation statement](#attestation-statement) produced in this format, defined using CDDL [\[RFC8610\]](#biblio-rfc8610 "Concise Data Definition Language (CDDL): A Notational Convention to Express Concise Binary Object Representation (CBOR) and JSON Data Structures") for the extension point `$$attStmtType` defined in [§ 6.5.4 Generating an Attestation Object](#sctn-generating-an-attestation-object).
- Signing procedure: The [signing procedure](#signing-procedure) for computing an [attestation statement](#attestation-statement) in this [format](#attestation-statement-format) given the [public key credential](#public-key-credential) to be attested, the [authenticator data](#authenticator-data) structure containing the authenticator data for the attestation, and the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data).
- Verification procedure: The procedure for verifying an [attestation statement](#attestation-statement), which takes the following verification procedure inputs:
	- attStmt: The [attestation statement](#attestation-statement) structure
		- authenticatorData: The [authenticator data](#authenticator-data) claimed to have been used for the attestation
		- clientDataHash: The [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data)
	The procedure returns either:
	- An error indicating that the attestation is invalid, or
		- An implementation-specific value representing the [attestation type](#attestation-type), and the [trust path](#attestation-trust-path). This attestation trust path is either empty (in case of [self attestation](#self-attestation)), or a set of X.509 certificates.

The initial list of specified [attestation statement formats](#attestation-statement-format) is in [§ 8 Defined Attestation Statement Formats](#sctn-defined-attestation-formats).

#### 6.5.3. Attestation Types

WebAuthn supports several [attestation types](#attestation-type), defining the semantics of [attestation statements](#attestation-statement) and their underlying trust models:

Note: This specification does not define any data structures explicitly expressing the [attestation types](#attestation-type) employed by [authenticators](#authenticator). [Relying Parties](#relying-party) engaging in [attestation statement](#attestation-statement) [verification](#verification-procedure) — i.e., when calling `navigator.credentials.create()` they select an [attestation conveyance](#attestation-conveyance) other than `none` and verify the received [attestation statement](#attestation-statement) — will determine the employed [attestation type](#attestation-type) as a part of [verification](#verification-procedure). See the "Verification procedure" subsections of [§ 8 Defined Attestation Statement Formats](#sctn-defined-attestation-formats). See also [§ 14.4.1 Attestation Privacy](#sctn-attestation-privacy). For all [attestation types](#attestation-type) defined in this section other than [Self](#self-attestation) and [None](#none), [Relying Party](#relying-party) [verification](#verification-procedure) is followed by matching the [trust path](#attestation-trust-path) to an acceptable root certificate per [step 23](#reg-ceremony-assess-trust) of [§ 7.1 Registering a New Credential](#sctn-registering-a-new-credential). Differentiating these [attestation types](#attestation-type) becomes useful primarily as a means for determining if the [attestation](#attestation) is acceptable under [Relying Party](#relying-party) policy.

Basic Attestation (Basic)

In the case of basic attestation [\[UAFProtocol\]](#biblio-uafprotocol "FIDO UAF Protocol Specification v1.0"), the authenticator’s [attestation key pair](#attestation-key-pair) is specific to an authenticator "model", i.e., a "batch" of authenticators. Thus, authenticators of the same, or similar, model often share the same [attestation key pair](#attestation-key-pair). See [§ 14.4.1 Attestation Privacy](#sctn-attestation-privacy) for further information.

[Basic attestation](#basic-attestation) is also referred to as batch attestation.

Self Attestation (Self)

In the case of [self attestation](#self-attestation), also known as surrogate basic attestation [\[UAFProtocol\]](#biblio-uafprotocol "FIDO UAF Protocol Specification v1.0"), the Authenticator does not have any specific [attestation key pair](#attestation-key-pair). Instead it uses the [credential private key](#credential-private-key) to create the [attestation signature](#attestation-signature). Authenticators without meaningful protection measures for an [attestation private key](#attestation-private-key) typically use this attestation type.

Attestation CA (AttCA)

In this case, an [authenticator](#authenticator) is based on a Trusted Platform Module (TPM) and holds an authenticator-specific "endorsement key" (EK). This key is used to securely communicate with a trusted third party, the [Attestation CA](#attestation-ca) [\[TCG-CMCProfile-AIKCertEnroll\]](#biblio-tcg-cmcprofile-aikcertenroll "TCG Infrastructure Working Group: A CMC Profile for AIK Certificate Enrollment") (formerly known as a "Privacy CA"). The [authenticator](#authenticator) can generate multiple attestation identity key pairs (AIK) and requests an [Attestation CA](#attestation-ca) to issue an AIK certificate for each. Using this approach, such an [authenticator](#authenticator) can limit the exposure of the EK (which is a global correlation handle) to Attestation CA(s). AIKs can be requested for each [authenticator](#authenticator) -generated [public key credential](#public-key-credential) individually, and conveyed to [Relying Parties](#relying-party) as [attestation certificates](#attestation-certificate).

Note: This concept typically leads to multiple attestation certificates. The attestation certificate requested most recently is called "active".

Anonymization CA (AnonCA)

In this case, the [authenticator](#authenticator) uses an [Anonymization CA](#anonymization-ca) which dynamically generates per- [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) [attestation certificates](#attestation-certificate) such that the [attestation statements](#attestation-statement) presented to [Relying Parties](#relying-party) do not provide uniquely identifiable information, e.g., that might be used for tracking purposes.

Note: [Attestation statements](#attestation-statement) conveying [attestations](#attestation) of [type](#attestation-type) [AttCA](#attca) or [AnonCA](#anonca) use the same data structure as those of [type](#attestation-type) [Basic](#basic), so the three attestation types are, in general, distinguishable only with externally provided knowledge regarding the contents of the [attestation certificates](#attestation-certificate) conveyed in the [attestation statement](#attestation-statement).

No attestation statement (None)

In this case, no attestation information is available. See also [§ 8.7 None Attestation Statement Format](#sctn-none-attestation).

#### 6.5.4. Generating an Attestation Object

To generate an [attestation object](#attestation-object) (see: [Figure 6](#fig-attStructs)) given:

attestationFormat

An [attestation statement format](#attestation-statement-format).

authData

A byte array containing [authenticator data](#authenticator-data).

hash

The [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data).

the [authenticator](#authenticator) MUST:

1. Let attStmt be the result of running attestationFormat ’s [signing procedure](#signing-procedure) given authData and hash.
2. Let fmt be attestationFormat ’s [attestation statement format identifier](#attestation-statement-format-identifier)
3. Return the [attestation object](#attestation-object) as a CBOR map with the following syntax, filled in with variables initialized by this algorithm:
	```
	attObj = {
	    authData: bytes,
	    ; Each choice in $$attStmtType defines the fmt value and attStmt structure
	    $$attStmtType
	} .within attStmtTemplate
	attStmtTemplate = {
	    authData: bytes,
	    fmt: text,
	    attStmt: (
	      { * tstr => any } ; Map is filled in by each concrete attStmtType
	      //
	      [ * any ]         ; attStmt may also be an array
	    )
	}
	```

#### 6.5.5. Signature Formats for Packed Attestation, FIDO U2F Attestation, and Assertion Signatures

- For COSEAlgorithmIdentifier -7 (ES256), and other ECDSA-based algorithms, the `sig` value MUST be encoded as an ASN.1 DER Ecdsa-Sig-Value, as defined in [\[RFC3279\]](#biblio-rfc3279 "Algorithms and Identifiers for the Internet X.509 Public Key Infrastructure Certificate and Certificate Revocation List (CRL) Profile") section 2.2.3.
	```
	Example:
	Note: Encoding lengths vary with INTEGER magnitude and curve size.
	30 43                                ; SEQUENCE (67 Bytes)
	    02 21                            ; INTEGER (33 Bytes)
	    | 00 89 90 95 04 e1 4f 1e 29 db a8 15 8f a7 c3 87
	    | e8 88 ff be 07 d8 24 bb 21 43 20 55 06 ab 15 9c
	    | 3e
	    02 1e                            ; INTEGER (30 Bytes)
	    | 56 55 4f b5 81 9b 12 84 5e 85 be 2f 78 37 1c f3
	    | cb 95 e3 87 f4 51 cb 36 2b 94 78 d1 83 d2
	```
	Note: As CTAP1/U2F [authenticators](#authenticator) are already producing signatures values in this format, CTAP2 [authenticators](#authenticator) will also produce signatures values in the same format, for consistency reasons.

It is RECOMMENDED that any new attestation formats defined not use ASN.1 encodings, but instead represent signatures as equivalent fixed-length byte arrays without internal structure, using the same representations as used by COSE signatures as defined in [\[RFC9053\]](#biblio-rfc9053 "CBOR Object Signing and Encryption (COSE): Initial Algorithms") and [\[RFC8230\]](#biblio-rfc8230 "Using RSA Algorithms with CBOR Object Signing and Encryption (COSE) Messages").

The below signature format definitions satisfy this requirement and serve as examples for deriving the same for other signature algorithms not explicitly mentioned here:

- For COSEAlgorithmIdentifier -257 (RS256), `sig` MUST contain the signature generated using the RSASSA-PKCS1-v1\_5 signature scheme defined in Section 8.2.1 of [\[RFC8017\]](#biblio-rfc8017 "PKCS #1: RSA Cryptography Specifications Version 2.2") with SHA-256 as the hash function. The signature is not ASN.1 wrapped.
- For COSEAlgorithmIdentifier -37 (PS256), `sig` MUST contain the signature generated using the RSASSA-PSS signature scheme defined in Section 8.1.1 of [\[RFC8017\]](#biblio-rfc8017 "PKCS #1: RSA Cryptography Specifications Version 2.2") with SHA-256 as the hash function. The signature is not ASN.1 wrapped.

## 7.

A [registration](#registration-ceremony) or [authentication ceremony](#authentication-ceremony) begins with the [WebAuthn Relying Party](#webauthn-relying-party) creating a `PublicKeyCredentialCreationOptions` or `PublicKeyCredentialRequestOptions` object, respectively, which encodes the parameters for the [ceremony](#ceremony). The [Relying Party](#relying-party) SHOULD take care to not leak sensitive information during this stage; see [§ 14.6.2 Username Enumeration](#sctn-username-enumeration) for details.

Upon successful execution of `create()` or `get()`, the [Relying Party](#relying-party) ’s script receives a `PublicKeyCredential` containing an `AuthenticatorAttestationResponse` or `AuthenticatorAssertionResponse` structure, respectively, from the client. It must then deliver the contents of this structure to the [Relying Party](#relying-party) server, using methods outside the scope of this specification. This section describes the operations that the [Relying Party](#relying-party) must perform upon receipt of these structures.

### 7.1. Registering a New Credential

In order to perform a [registration ceremony](#registration-ceremony), the [Relying Party](#relying-party) MUST proceed as follows:

1. Let options be a new `CredentialCreationOptions` structure configured to the [Relying Party](#relying-party) ’s needs for the ceremony. Let pkOptions be `` options.`publicKey` ``.
2. Call `navigator.credentials.create()` and pass options as the argument. Let credential be the result of the successfully resolved promise. If the promise is rejected, abort the ceremony with a user-visible error, or otherwise guide the user experience as might be determinable from the context available in the rejected promise. For example if the promise is rejected with an error code equivalent to " `InvalidStateError` ", the user might be instructed to use a different [authenticator](#authenticator). For information on different error contexts and the circumstances leading to them, see [§ 6.3.2 The authenticatorMakeCredential Operation](#sctn-op-make-cred).
3. Let response be `` credential.`response` ``. If response is not an instance of `AuthenticatorAttestationResponse`, abort the ceremony with a user-visible error.
4. Let clientExtensionResults be the result of calling `` credential.`getClientExtensionResults()` ``.
5. Let JSONtext be the result of running [UTF-8 decode](https://encoding.spec.whatwg.org/#utf-8-decode) on the value of `` response.`clientDataJSON` ``.
	Note: Using any implementation of [UTF-8 decode](https://encoding.spec.whatwg.org/#utf-8-decode) is acceptable as long as it yields the same result as that yielded by the [UTF-8 decode](https://encoding.spec.whatwg.org/#utf-8-decode) algorithm. In particular, any leading byte order mark (BOM) must be stripped.
6. Let C, the [client data](#client-data) claimed as collected during the credential creation, be the result of running an implementation-specific JSON parser on JSONtext.
	Note: C may be any implementation-specific data structure representation, as long as C ’s components are referenceable, as required by this algorithm.
7. Verify that the value of `` C.`type` `` is `webauthn.create`.
8. Verify that the value of `` C.`challenge` `` equals the base64url encoding of `` pkOptions.`challenge` ``.
9. If `` C.`crossOrigin` `` is present and set to `true`, verify that the [Relying Party](#relying-party) expects that this credential would have been created within an iframe that is not [same-origin with its ancestors](https://w3c.github.io/webappsec-credential-management/#same-origin-with-its-ancestors).
10. If `` C.`topOrigin` `` is present:
	1. Verify that the [Relying Party](#relying-party) expects that this credential would have been created within an iframe that is not [same-origin with its ancestors](https://w3c.github.io/webappsec-credential-management/#same-origin-with-its-ancestors).
		2. Verify that the value of `` C.`topOrigin` `` matches the [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) of a page that the [Relying Party](#relying-party) expects to be sub-framed within. See [§ 13.4.9 Validating the origin of a credential](#sctn-validating-origin) for guidance.
11. Let hash be the result of computing a hash over `` response.`clientDataJSON` `` using SHA-256.
12. Perform CBOR decoding on the `attestationObject` field of the `AuthenticatorAttestationResponse` structure to obtain the attestation statement format fmt, the [authenticator data](#authenticator-data) authData, and the attestation statement attStmt.
13. Verify that the `rpIdHash` in authData is the SHA-256 hash of the [RP ID](#rp-id) expected by the [Relying Party](#relying-party).
14. If `` options.`mediation` `` is not set to `conditional`, verify that the [UP](#authdata-flags-up) bit of the `flags` in authData is set.
15. If the [Relying Party](#relying-party) requires [user verification](#user-verification) for this registration, verify that the [UV](#authdata-flags-uv) bit of the `flags` in authData is set.
16. If the [BE](#authdata-flags-be) bit of the `flags` in authData is not set, verify that the [BS](#authdata-flags-bs) bit is not set.
17. If the [Relying Party](#relying-party) uses the credential’s [backup eligibility](#backup-eligibility) to inform its user experience flows and/or policies, evaluate the [BE](#authdata-flags-be) bit of the `flags` in authData.
18. If the [Relying Party](#relying-party) uses the credential’s [backup state](#backup-state) to inform its user experience flows and/or policies, evaluate the [BS](#authdata-flags-bs) bit of the `flags` in authData.
19. Verify that the "alg" parameter in the [credential public key](#authdata-attestedcredentialdata-credentialpublickey) in authData matches the `alg` attribute of one of the [items](https://infra.spec.whatwg.org/#list-item) in `` pkOptions.`pubKeyCredParams` ``.
20. Determine the attestation statement format by performing a USASCII case-sensitive match on fmt against the set of supported WebAuthn Attestation Statement Format Identifier values. An up-to-date list of registered WebAuthn Attestation Statement Format Identifier values is maintained in the IANA "WebAuthn Attestation Statement Format Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)").
21. Verify that attStmt is a correct [attestation statement](#attestation-statement), conveying a valid [attestation signature](#attestation-signature), by using the [attestation statement format](#attestation-statement-format) fmt ’s [verification procedure](#verification-procedure) given attStmt, authData and hash.
	Note: Each [attestation statement format](#attestation-statement-format) specifies its own [verification procedure](#verification-procedure). See [§ 8 Defined Attestation Statement Formats](#sctn-defined-attestation-formats) for the initially-defined formats, and [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") for the up-to-date list.
22. If validation is successful, obtain a list of acceptable trust anchors (i.e. attestation root certificates) for that attestation type and attestation statement format fmt, from a trusted source or from policy. For example, the FIDO Metadata Service [\[FIDOMetadataService\]](#biblio-fidometadataservice "FIDO Metadata Service") provides one way to obtain such information, using the `aaguid` in the `attestedCredentialData` in authData.
23. Assess the attestation trustworthiness using the outputs of the [verification procedure](#verification-procedure) in [step 21](#reg-ceremony-verify-attestation), as follows:
	- If [no attestation](#none) was provided, verify that [None](#none) attestation is acceptable under [Relying Party](#relying-party) policy.
		- If [self attestation](#self-attestation) was used, verify that [self attestation](#self-attestation) is acceptable under [Relying Party](#relying-party) policy.
		- Otherwise, use the X.509 certificates returned as the [attestation trust path](#attestation-trust-path) from the [verification procedure](#verification-procedure) to verify that the attestation public key either correctly chains up to an acceptable root certificate, or is itself an acceptable certificate (i.e., it and the root certificate obtained in [step 22](#reg-ceremony-attestation-trust-anchors) may be the same).
	If the attestation statement is not deemed trustworthy, the [Relying Party](#relying-party) SHOULD fail the [registration ceremony](#registration-ceremony).
	NOTE: However, if permitted by policy, the [Relying Party](#relying-party) MAY register the [credential ID](#credential-id) and credential public key but treat the credential as one with [self attestation](#self-attestation) (see [§ 6.5.3 Attestation Types](#sctn-attestation-types)). If doing so, the [Relying Party](#relying-party) is asserting there is no cryptographic proof that the [public key credential](#public-key-credential) has been generated by a particular [authenticator](#authenticator) model. See [\[FIDOSecRef\]](#biblio-fidosecref "FIDO Security Reference") and [\[UAFProtocol\]](#biblio-uafprotocol "FIDO UAF Protocol Specification v1.0") for a more detailed discussion.
24. Verify that the `credentialId` is ≤ 1023 bytes. Credential IDs larger than this many bytes SHOULD cause the RP to fail this [registration ceremony](#registration-ceremony).
25. Verify that the `credentialId` is not yet registered for any user. If the `credentialId` is already known then the [Relying Party](#relying-party) SHOULD fail this [registration ceremony](#registration-ceremony).
	Note: The rationale for [Relying Parties](#relying-party) rejecting duplicate [credential IDs](#credential-id) is as follows: [credential IDs](#credential-id) contain sufficient entropy that accidental duplication is very unlikely. However, [attestation types](#attestation-type) other than [self attestation](#self-attestation) do not include a self-signature to explicitly prove possession of the [credential private key](#credential-private-key) at [registration](#registration) time. Thus an attacker who has managed to obtain a user’s [credential ID](#credential-id) and [credential public key](#credential-public-key) for a site (this could be potentially accomplished in various ways), could attempt to register a victim’s credential as their own at that site. If the [Relying Party](#relying-party) accepts this new registration and replaces the victim’s existing credential registration, and the [credentials are discoverable](#discoverable-credential), then the victim could be forced to sign into the attacker’s account at their next attempt. Data saved to the site by the victim in that state would then be available to the attacker.
26. Let credentialRecord be a new [credential record](#credential-record) with the following contents:
	[type](#abstract-opdef-credential-record-type)
	`` credential.`type` ``.
	[id](#abstract-opdef-credential-record-id)
	`` credential.`id` `` or `` credential.`rawId` ``, whichever format is preferred by the [Relying Party](#relying-party).
	[publicKey](#abstract-opdef-credential-record-publickey)
	The [credential public key](#credential-public-key) in authData.
	[signCount](#abstract-opdef-credential-record-signcount)
	`authData.signCount`.
	[uvInitialized](#abstract-opdef-credential-record-uvinitialized)
	The value of the [UV](#authdata-flags-uv) [flag](#authdata-flags) in authData.
	[transports](#abstract-opdef-credential-record-transports)
	The value returned from `` response.`getTransports()` ``.
	[backupEligible](#abstract-opdef-credential-record-backupeligible)
	The value of the [BE](#authdata-flags-be) [flag](#authdata-flags) in authData.
	[backupState](#abstract-opdef-credential-record-backupstate)
	The value of the [BS](#authdata-flags-bs) [flag](#authdata-flags) in authData.
	The new [credential record](#credential-record) MAY also include the following OPTIONAL contents:
	[attestationObject](#abstract-opdef-credential-record-attestationobject)
	`` response.`attestationObject` ``.
	[attestationClientDataJSON](#abstract-opdef-credential-record-attestationclientdatajson)
	`` response.`clientDataJSON` ``.
	The [Relying Party](#relying-party) MAY also include any additional [items](https://infra.spec.whatwg.org/#struct-item) as necessary. As a non-normative example, the [Relying Party](#relying-party) might allow the user to set a "nickname" for the credential to help the user remember which [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) is [bound](#bound-credential) to which [authenticator](#authenticator) when interacting with account settings.
27. Process the [client extension outputs](#client-extension-output) in clientExtensionResults and the [authenticator extension outputs](#authenticator-extension-output) in the `extensions` in authData as required by the [Relying Party](#relying-party). Depending on each [extension](#webauthn-extensions), processing steps may be concretely specified or it may be up to the [Relying Party](#relying-party) what to do with extension outputs. The [Relying Party](#relying-party) MAY ignore any or all extension outputs.
	[Clients](#client) MAY set additional [authenticator extensions](#authenticator-extension) or [client extensions](#client-extension) and thus cause values to appear in the [authenticator extension outputs](#authenticator-extension-output) or [client extension outputs](#client-extension-output) that were not requested by the [Relying Party](#relying-party) in `` pkOptions.`extensions` ``. The [Relying Party](#relying-party) MUST be prepared to handle such situations, whether by ignoring the unsolicited extensions or by rejecting the attestation. The [Relying Party](#relying-party) can make this decision based on local policy and the extensions in use.
	Since all extensions are OPTIONAL for both the [client](#client) and the [authenticator](#authenticator), the [Relying Party](#relying-party) MUST also be prepared to handle cases where none or not all of the requested extensions were acted upon.
28. If all the above steps are successful, store credentialRecord in the [user account](#user-account) that was denoted in `` pkOptions.`user` `` and continue the [registration ceremony](#registration-ceremony) as appropriate. Otherwise, fail the [registration ceremony](#registration-ceremony).
	If the [Relying Party](#relying-party) does not fail the [registration ceremony](#registration-ceremony) in this case, then the [Relying Party](#relying-party) is accepting that there is no cryptographic proof that the [public key credential](#public-key-credential) has been generated by any particular [authenticator](#authenticator) model. The [Relying Party](#relying-party) MAY consider the credential as equivalent to one with [no attestation](#none) (see [§ 6.5.3 Attestation Types](#sctn-attestation-types)). See [\[FIDOSecRef\]](#biblio-fidosecref "FIDO Security Reference") and [\[UAFProtocol\]](#biblio-uafprotocol "FIDO UAF Protocol Specification v1.0") for a more detailed discussion.
	Verification of [attestation objects](#attestation-object) requires that the [Relying Party](#relying-party) has a trusted method of determining acceptable trust anchors in [step 22](#reg-ceremony-attestation-trust-anchors) above. Also, if certificates are being used, the [Relying Party](#relying-party) MUST have access to certificate status information for the intermediate CA certificates. The [Relying Party](#relying-party) MUST also be able to build the attestation certificate chain if the client did not provide this chain in the attestation information.

### 7.2. Verifying an Authentication Assertion

In order to perform an [authentication ceremony](#authentication-ceremony), the [Relying Party](#relying-party) MUST proceed as follows:

1. Let options be a new `CredentialRequestOptions` structure configured to the [Relying Party](#relying-party) ’s needs for the ceremony. Let pkOptions be `` options.`publicKey` ``.
2. Call `navigator.credentials.get()` and pass options as the argument. Let credential be the result of the successfully resolved promise. If the promise is rejected, abort the ceremony with a user-visible error, or otherwise guide the user experience as might be determinable from the context available in the rejected promise. For information on different error contexts and the circumstances leading to them, see [§ 6.3.3 The authenticatorGetAssertion Operation](#sctn-op-get-assertion).
3. Let response be `` credential.`response` ``. If response is not an instance of `AuthenticatorAssertionResponse`, abort the ceremony with a user-visible error.
4. Let clientExtensionResults be the result of calling `` credential.`getClientExtensionResults()` ``.
5. If `` pkOptions.`allowCredentials` `` [is not empty](https://infra.spec.whatwg.org/#list-is-empty), verify that `` credential.`id` `` identifies one of the [public key credentials](#public-key-credential) listed in `` pkOptions.`allowCredentials` ``.
6. Identify the user being authenticated and let credentialRecord be the [credential record](#credential-record) for the [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential):
	If the user was identified before the [authentication ceremony](#authentication-ceremony) was initiated, e.g., via a username or cookie,
	verify that the identified [user account](#user-account) contains a [credential record](#credential-record) whose [id](#abstract-opdef-credential-record-id) equals `` credential.`rawId` ``. Let credentialRecord be that [credential record](#credential-record). If `` response.`userHandle` `` is present, verify that it equals the [user handle](#user-handle) of the [user account](#user-account).
	If the user was not identified before the [authentication ceremony](#authentication-ceremony) was initiated,
	verify that `` response.`userHandle` `` is present. Verify that the [user account](#user-account) identified by `` response.`userHandle` `` contains a [credential record](#credential-record) whose [id](#abstract-opdef-credential-record-id) equals `` credential.`rawId` ``. Let credentialRecord be that [credential record](#credential-record).
7. Let cData, authData and sig denote the value of response ’s `clientDataJSON`, `authenticatorData`, and `signature` respectively.
8. Let JSONtext be the result of running [UTF-8 decode](https://encoding.spec.whatwg.org/#utf-8-decode) on the value of cData.
	Note: Using any implementation of [UTF-8 decode](https://encoding.spec.whatwg.org/#utf-8-decode) is acceptable as long as it yields the same result as that yielded by the [UTF-8 decode](https://encoding.spec.whatwg.org/#utf-8-decode) algorithm. In particular, any leading byte order mark (BOM) must be stripped.
9. Let C, the [client data](#client-data) claimed as used for the signature, be the result of running an implementation-specific JSON parser on JSONtext.
	Note: C may be any implementation-specific data structure representation, as long as C ’s components are referenceable, as required by this algorithm.
10. Verify that the value of `` C.`type` `` is the string `webauthn.get`.
11. Verify that the value of `` C.`challenge` `` equals the base64url encoding of `` pkOptions.`challenge` ``.
12. Verify that the value of `` C.`origin` `` is an [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) expected by the [Relying Party](#relying-party). See [§ 13.4.9 Validating the origin of a credential](#sctn-validating-origin) for guidance.
13. If `` C.`crossOrigin` `` is present and set to `true`, verify that the [Relying Party](#relying-party) expects this credential to be used within an iframe that is not [same-origin with its ancestors](https://w3c.github.io/webappsec-credential-management/#same-origin-with-its-ancestors).
14. If `` C.`topOrigin` `` is present:
	1. Verify that the [Relying Party](#relying-party) expects this credential to be used within an iframe that is not [same-origin with its ancestors](https://w3c.github.io/webappsec-credential-management/#same-origin-with-its-ancestors).
		2. Verify that the value of `` C.`topOrigin` `` matches the [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) of a page that the [Relying Party](#relying-party) expects to be sub-framed within. See [§ 13.4.9 Validating the origin of a credential](#sctn-validating-origin) for guidance.
15. Verify that the `rpIdHash` in authData is the SHA-256 hash of the [RP ID](#rp-id) expected by the [Relying Party](#relying-party).
	Note: If using the [appid](#appid) extension, this step needs some special logic. See [§ 10.1.1 FIDO AppID Extension (appid)](#sctn-appid-extension) for details.
16. Verify that the [UP](#authdata-flags-up) bit of the `flags` in authData is set.
17. Determine whether [user verification](#user-verification) is required for this assertion. [User verification](#user-verification) SHOULD be required if, and only if, `` pkOptions.`userVerification` `` is set to `required`.
	If [user verification](#user-verification) was determined to be required, verify that the [UV](#authdata-flags-uv) bit of the `flags` in authData is set. Otherwise, ignore the value of the [UV](#authdata-flags-uv) [flag](#authdata-flags).
18. If the [BE](#authdata-flags-be) bit of the `flags` in authData is not set, verify that the [BS](#authdata-flags-bs) bit is not set.
19. If the credential [backup state](#backup-state) is used as part of [Relying Party](#relying-party) business logic or policy, let currentBe and currentBs be the values of the [BE](#authdata-flags-be) and [BS](#authdata-flags-bs) bits, respectively, of the `flags` in authData. Compare currentBe and currentBs with `credentialRecord.backupEligible` and `credentialRecord.backupState`:
	1. If `credentialRecord.backupEligible` is set, verify that currentBe is set.
		2. If `credentialRecord.backupEligible` is not set, verify that currentBe is not set.
		3. Apply [Relying Party](#relying-party) policy, if any.
	Note: See [§ 6.1.3 Credential Backup State](#sctn-credential-backup) for examples of how a [Relying Party](#relying-party) might process the [BS](#authdata-flags-bs) [flag](#authdata-flags) values.
20. Let hash be the result of computing a hash over the cData using SHA-256.
21. Using `credentialRecord.publicKey`, verify that sig is a valid signature over the binary concatenation of authData and hash.
	Note: This verification step is compatible with signatures generated by FIDO U2F authenticators. See [§ 6.1.2 FIDO U2F Signature Format Compatibility](#sctn-fido-u2f-sig-format-compat).
22. If authData.`signCount` is nonzero or `credentialRecord.signCount` is nonzero, then run the following sub-step:
	- If authData.`signCount` is
		greater than `credentialRecord.signCount`:
		The signature counter is valid.
		less than or equal to `credentialRecord.signCount`:
		This is a signal, but not proof, that the authenticator may be cloned. For example it might mean that:
		- Two or more copies of the [credential private key](#credential-private-key) may exist and are being used in parallel.
		- An authenticator is malfunctioning.
		- A race condition exists where the [Relying Party](#relying-party) is processing assertion responses in an order other than the order they were generated at the authenticator.
		[Relying Parties](#relying-party) should evaluate their own operational characteristics and incorporate this information into their risk scoring. Whether the [Relying Party](#relying-party) updates `credentialRecord.signCount` below in this case, or not, or fails the [authentication ceremony](#authentication-ceremony) or not, is [Relying Party](#relying-party) -specific.
		For more information on signature counter considerations, see [§ 6.1.1 Signature Counter Considerations](#sctn-sign-counter).
23. Process the [client extension outputs](#client-extension-output) in clientExtensionResults and the [authenticator extension outputs](#authenticator-extension-output) in the `extensions` in authData as required by the [Relying Party](#relying-party). Depending on each [extension](#webauthn-extensions), processing steps may be concretely specified or it may be up to the [Relying Party](#relying-party) what to do with extension outputs. The [Relying Party](#relying-party) MAY ignore any or all extension outputs.
	[Clients](#client) MAY set additional [authenticator extensions](#authenticator-extension) or [client extensions](#client-extension) and thus cause values to appear in the [authenticator extension outputs](#authenticator-extension-output) or [client extension outputs](#client-extension-output) that were not requested by the [Relying Party](#relying-party) in `` pkOptions.`extensions` ``. The [Relying Party](#relying-party) MUST be prepared to handle such situations, whether by ignoring the unsolicited extensions or by rejecting the assertion. The [Relying Party](#relying-party) can make this decision based on local policy and the extensions in use.
	Since all extensions are OPTIONAL for both the [client](#client) and the [authenticator](#authenticator), the [Relying Party](#relying-party) MUST also be prepared to handle cases where none or not all of the requested extensions were acted upon.
24. Update credentialRecord with new state values:
	1. Update `credentialRecord.signCount` to the value of authData.`signCount`.
		2. Update `credentialRecord.backupState` to the value of currentBs.
		3. If `credentialRecord.uvInitialized` is `false`, update it to the value of the [UV](#authdata-flags-uv) bit in the [flags](#authdata-flags) in authData. This change SHOULD require authorization by an additional [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) equivalent to WebAuthn [user verification](#user-verification); if not authorized, skip this step.
	If the [Relying Party](#relying-party) performs additional security checks beyond these WebAuthn [authentication ceremony](#authentication-ceremony) steps, the above state updates SHOULD be deferred to after those additional checks are completed successfully.
25. If all the above steps are successful, continue the [authentication ceremony](#authentication-ceremony) as appropriate. Otherwise, fail the [authentication ceremony](#authentication-ceremony).

## 8\. Defined Attestation Statement Formats

WebAuthn supports pluggable attestation statement formats. This section defines an initial set of such formats.

### 8.1. Attestation Statement Format Identifiers

Attestation statement formats are identified by a string, called an attestation statement format identifier, chosen by the author of the [attestation statement format](#attestation-statement-format).

Attestation statement format identifiers SHOULD be registered in the IANA "WebAuthn Attestation Statement Format Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)"). All registered attestation statement format identifiers are unique amongst themselves as a matter of course.

Unregistered attestation statement format identifiers SHOULD use lowercase reverse domain-name naming, using a domain name registered by the developer, in order to assure uniqueness of the identifier. All attestation statement format identifiers MUST be a maximum of 32 octets in length and MUST consist only of printable USASCII characters, excluding backslash and doublequote, i.e., VCHAR as defined in [\[RFC5234\]](#biblio-rfc5234 "Augmented BNF for Syntax Specifications: ABNF") but without %x22 and %x5c.

Note: This means attestation statement format identifiers based on domain names are restricted to incorporating only LDH Labels [\[RFC5890\]](#biblio-rfc5890 "Internationalized Domain Names for Applications (IDNA): Definitions and Document Framework").

Implementations MUST match WebAuthn attestation statement format identifiers in a case-sensitive fashion.

Attestation statement formats that may exist in multiple versions SHOULD include a version in their identifier. In effect, different versions are thus treated as different formats, e.g., `packed2` as a new version of the [§ 8.2 Packed Attestation Statement Format](#sctn-packed-attestation).

The following sections present a set of currently-defined and registered attestation statement formats and their identifiers. The up-to-date list of registered [attestation statement format identifiers](#attestation-statement-format-identifier) is maintained in the IANA "WebAuthn Attestation Statement Format Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)").

### 8.2. Packed Attestation Statement Format

This is a WebAuthn optimized attestation statement format. It uses a very compact but still extensible encoding method. It is implementable by [authenticators](#authenticator) with limited resources (e.g., secure elements).

Attestation statement format identifier

packed

Attestation types supported

[Basic](#basic), [Self](#self), [AttCA](#attca)

Syntax

The syntax of a Packed Attestation statement is defined by the following CDDL:

```
$$attStmtType //= (
                      fmt: "packed",
                      attStmt: packedStmtFormat
                  )

packedStmtFormat = {
                       alg: COSEAlgorithmIdentifier,
                       sig: bytes,
                       x5c: [ attestnCert: bytes, * (caCert: bytes) ]
                   } //
                   {
                       alg: COSEAlgorithmIdentifier
                       sig: bytes,
                   }
```

The semantics of the fields are as follows:

alg

A `COSEAlgorithmIdentifier` containing the identifier of the algorithm used to generate the [attestation signature](#attestation-signature).

sig

A byte string containing the [attestation signature](#attestation-signature).

x5c

The elements of this array contain attestnCert and its certificate chain (if any), each encoded in X.509 format. The attestation certificate attestnCert MUST be the first element in the array.

attestnCert

The attestation certificate, encoded in X.509 format.

Signing procedure

The signing procedure for this attestation statement format is similar to [the procedure for generating assertion signatures](#fig-signature).

1. Let authenticatorData denote the [authenticator data for the attestation](#authenticator-data-for-the-attestation), and let clientDataHash denote the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data).
2. If [Basic](#basic) or [AttCA](#attca) [attestation](#attestation) is in use, the authenticator produces the sig by concatenating authenticatorData and clientDataHash, and signing the result using an [attestation private key](#attestation-private-key) selected through an authenticator-specific mechanism. It sets x5c to attestnCert followed by the related certificate chain (if any). It sets alg to the algorithm of the attestation private key.
3. If [self attestation](#self-attestation) is in use, the authenticator produces sig by concatenating authenticatorData and clientDataHash, and signing the result using the credential private key. It sets alg to the algorithm of the credential private key and omits the other fields.

Verification procedure

Given the [verification procedure inputs](#verification-procedure-inputs) attStmt, authenticatorData and clientDataHash, the [verification procedure](#verification-procedure) is as follows:

1. Verify that attStmt is valid CBOR conforming to the syntax defined above and perform CBOR decoding on it to extract the contained fields.
2. If x5c is present:
	- Verify that sig is a valid signature over the concatenation of authenticatorData and clientDataHash using the attestation public key in attestnCert with the algorithm specified in alg.
		- Verify that attestnCert meets the requirements in [§ 8.2.1 Certificate Requirements for Packed Attestation Statements](#sctn-packed-attestation-cert-requirements).
		- If attestnCert contains an extension with OID `1.3.6.1.4.1.45724.1.1.4` (`id-fido-gen-ce-aaguid`) verify that the value of this extension matches the `aaguid` in authenticatorData.
		- Optionally, inspect x5c and consult externally provided knowledge to determine whether attStmt conveys a [Basic](#basic) or [AttCA](#attca) attestation.
		- If successful, return implementation-specific values representing [attestation type](#attestation-type) [Basic](#basic), [AttCA](#attca) or uncertainty, and [attestation trust path](#attestation-trust-path) x5c.
3. If x5c is not present, [self attestation](#self-attestation) is in use.
	- Validate that alg matches the algorithm of the `credentialPublicKey` in authenticatorData.
		- Verify that sig is a valid signature over the concatenation of authenticatorData and clientDataHash using the credential public key with alg.
		- If successful, return implementation-specific values representing [attestation type](#attestation-type) [Self](#self) and an empty [attestation trust path](#attestation-trust-path).

#### 8.2.1. Certificate Requirements for Packed Attestation Statements

The attestation certificate MUST have the following fields/extensions:

- Version MUST be set to 3 (which is indicated by an ASN.1 INTEGER with value 2).
- Subject field MUST be set to:
	Subject-C
	ISO 3166 code specifying the country where the Authenticator vendor is incorporated (PrintableString)
	Subject-O
	Legal name of the Authenticator vendor (UTF8String)
	Subject-OU
	Literal string “Authenticator Attestation” (UTF8String)
	Subject-CN
	A UTF8String of the vendor’s choosing
- If the related attestation root certificate is used for multiple authenticator models, the Extension OID `1.3.6.1.4.1.45724.1.1.4` (`id-fido-gen-ce-aaguid`) MUST be present, containing the AAGUID as a 16-byte OCTET STRING. The extension MUST NOT be marked as critical.
	As [Relying Parties](#relying-party) may not know if the attestation root certificate is used for multiple authenticator models, it is suggested that [Relying Parties](#relying-party) check if the extension is present, and if it is, then validate that it contains that same AAGUID as presented in the [attestation object](#attestation-object).
	Note that an X.509 Extension encodes the DER-encoding of the value in an OCTET STRING. Thus, the AAGUID MUST be wrapped in *two* OCTET STRINGS to be valid.
- The Basic Constraints extension MUST have the CA component set to `false`.

Additionally, an Authority Information Access (AIA) extension with entry `id-ad-ocsp` and a CRL Distribution Point extension [\[RFC5280\]](#biblio-rfc5280 "Internet X.509 Public Key Infrastructure Certificate and Certificate Revocation List (CRL) Profile") are both OPTIONAL as the status of many attestation certificates is available through authenticator metadata services. See, for example, the FIDO Metadata Service [\[FIDOMetadataService\]](#biblio-fidometadataservice "FIDO Metadata Service").

The firmware of a particular authenticator model MAY be differentiated using the Extension OID `1.3.6.1.4.1.45724.1.1.5` (`id-fido-gen-ce-fw-version`). When present, this attribute contains an INTEGER with a non-negative value which is incremented for new firmware release versions. The extension MUST NOT be marked as critical.

For example, the following is an attestation certificate containing the above extension OIDs as well as required fields:

```
-----BEGIN CERTIFICATE-----
MIIBzTCCAXOgAwIBAgIUYHS3FJEL/JTfFqafuAHvlAS+hDYwCgYIKoZIzj0EAwIw
QTELMAkGA1UEBhMCVVMxFDASBgNVBAoMC1dlYkF1dGhuIFdHMRwwGgYDVQQDDBNF
eGFtcGxlIEF0dGVzdGF0aW9uMCAXDTI0MDEwMzE3NDUyMVoYDzIwNTAwMTA2MTc0
NTIxWjBBMQswCQYDVQQGEwJVUzEUMBIGA1UECgwLV2ViQXV0aG4gV0cxHDAaBgNV
BAMME0V4YW1wbGUgQXR0ZXN0YXRpb24wWTATBgcqhkjOPQIBBggqhkjOPQMBBwNC
AATDQN9uaFFH4BKBjthHTM1drpb7gIuPod67qyF6UdL4qah6XUp6tE7Prl+DfQ7P
YH9yMOOcci3nr+Q/jOBaWVERo0cwRTAhBgsrBgEEAYLlHAEBBAQSBBDNjDlcJu3u
3mU7AHl9A8o8MBIGCysGAQQBguUcAQEFBAMCASowDAYDVR0TAQH/BAIwADAKBggq
hkjOPQQDAgNIADBFAiA3k3aAUVtLhDHLXOgY2kRnK2hrbRgf2EKdTDLJ1Ds/RAIh
AOmIblhI3ALCHOaO0IO7YlMpw/lSTvFYv3qwO3m7H8Dc
-----END CERTIFICATE-----
```

The attributes above are structured within this certificate as such:

```
30 21                                    -- SEQUENCE
  06 0B 2B 06 01 04 01 82 E5 1C 01 01 04 -- OID 1.3.6.1.4.1.45724.1.1.4
  04 12                                  -- OCTET STRING
    04  10                               -- OCTET STRING
      CD 8C 39 5C 26 ED EE DE            -- AAGUID cd8c395c-26ed-eede-653b-00797d03ca3c
      65 3B 00 79 7D 03 CA 3C 

30 12                                    -- SEQUENCE
  06 0B 2B 06 01 04 01 82 E5 1C 01 01 05 -- OID 1.3.6.1.4.1.45724.1.1.5
  04 03                                  -- OCTET STRING
    02 01                                -- INTEGER
      2A                                 -- Firmware version: 42
```

#### 8.2.2. Certificate Requirements for Enterprise Packed Attestation Statements

The Extension OID `1.3.6.1.4.1.45724.1.1.2` ( `id-fido-gen-ce-sernum` ) MAY additionally be present in packed attestations for enterprise use. If present, this extension MUST indicate a unique octet string value per device against a particular AAGUID. This value MUST remain constant through factory resets, but MAY be distinct from any other serial number or other hardware identifier associated with the device. This extension MUST NOT be marked as critical, and the corresponding value is encoded as an OCTET STRING. This extension MUST NOT be present in non-enterprise attestations.

### 8.3. TPM Attestation Statement Format

This attestation statement format is generally used by authenticators that use a Trusted Platform Module as their cryptographic engine.

Attestation statement format identifier

tpm

Attestation types supported

[AttCA](#attca)

Syntax

The syntax of a TPM Attestation statement is as follows:

```
$$attStmtType // = (
                       fmt: "tpm",
                       attStmt: tpmStmtFormat
                   )

tpmStmtFormat = {
                    ver: "2.0",
                    (
                        alg: COSEAlgorithmIdentifier,
                        x5c: [ aikCert: bytes, * (caCert: bytes) ]
                    )
                    sig: bytes,
                    certInfo: bytes,
                    pubArea: bytes
                }
```

The semantics of the above fields are as follows:

ver

The version of the TPM specification to which the signature conforms.

alg

A `COSEAlgorithmIdentifier` containing the identifier of the algorithm used to generate the [attestation signature](#attestation-signature).

x5c

aikCert followed by its certificate chain, in X.509 encoding.

aikCert

The AIK certificate used for the attestation, in X.509 encoding.

sig

The [attestation signature](#attestation-signature), in the form of a TPMT\_SIGNATURE structure as specified in [\[TPMv2-Part2\]](#biblio-tpmv2-part2 "Trusted Platform Module Library, Part 2: Structures") section 11.3.4.

certInfo

The TPMS\_ATTEST structure over which the above signature was computed, as specified in [\[TPMv2-Part2\]](#biblio-tpmv2-part2 "Trusted Platform Module Library, Part 2: Structures") section 10.12.8.

pubArea

The TPMT\_PUBLIC structure (see [\[TPMv2-Part2\]](#biblio-tpmv2-part2 "Trusted Platform Module Library, Part 2: Structures") section 12.2.4) used by the TPM to represent the credential public key.

Signing procedure

Let authenticatorData denote the [authenticator data for the attestation](#authenticator-data-for-the-attestation), and let clientDataHash denote the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data).

Concatenate authenticatorData and clientDataHash to form attToBeSigned.

Generate a signature using the procedure specified in [\[TPMv2-Part3\]](#biblio-tpmv2-part3 "Trusted Platform Module Library, Part 3: Commands") Section 18.2, using the attestation private key and setting the `extraData` parameter to the digest of attToBeSigned using the hash algorithm corresponding to the "alg" signature algorithm. (For the "RS256" algorithm, this would be a SHA-256 digest.)

Set the pubArea field to the public area of the credential public key (the TPMT\_PUBLIC structure), the certInfo field (the TPMS\_ATTEST structure) to the output parameter of the same name, and the sig field to the signature obtained from the above procedure.

Note: If the pubArea is read from the TPM using the TPM2\_ReadPublic command, that command returns a TPM2B\_PUBLIC structure. TPM2B\_PUBLIC is two bytes of length followed by the TPMT\_PUBLIC structure. The two bytes of length must be removed prior to putting this into the pubArea.

Verification procedure

Given the [verification procedure inputs](#verification-procedure-inputs) attStmt, authenticatorData and clientDataHash, the [verification procedure](#verification-procedure) is as follows:

Verify that attStmt is valid CBOR conforming to the syntax defined above and perform CBOR decoding on it to extract the contained fields.

Verify that the public key specified by the `parameters` and `unique` fields of pubArea is identical to the `credentialPublicKey` in the `attestedCredentialData` in authenticatorData.

Concatenate authenticatorData and clientDataHash to form attToBeSigned.

Verify integrity of certInfo

- Verify that x5c is present.
- Verify that aikCert meets the requirements in [§ 8.3.1 TPM Attestation Statement Certificate Requirements](#sctn-tpm-cert-requirements).
- If aikCert contains an extension with OID `1.3.6.1.4.1.45724.1.1.4` (`id-fido-gen-ce-aaguid`) verify that the value of this extension matches the `aaguid` in authenticatorData.
- Verify the sig is a valid signature over certInfo using the attestation public key in aikCert with the algorithm specified in alg.

Validate that certInfo is valid: Note: certInfo is a TPMS\_ATTEST structure.

- Verify that `magic` is set to `TPM_GENERATED_VALUE`.
- Verify that `type` is set to `TPM_ST_ATTEST_CERTIFY`.
- Verify that `extraData` is set to the hash of attToBeSigned using the hash algorithm employed in "alg".
- Verify that `attested` contains a `TPMS_CERTIFY_INFO` structure as specified in [\[TPMv2-Part2\]](#biblio-tpmv2-part2 "Trusted Platform Module Library, Part 2: Structures") section 10.12.3, whose `name` field contains a valid Name for pubArea, as computed using the procedure specified in [\[TPMv2-Part1\]](#biblio-tpmv2-part1 "Trusted Platform Module Library, Part 1: Architecture") section 16 using the nameAlg in the pubArea.
	Note: The TPM will always return TPMS\_CERTIFY\_INFO structure with the same nameAlg in the `name` as the nameAlg in pubArea.
	Note: The remaining fields in the "Standard Attestation Structure" [\[TPMv2-Part1\]](#biblio-tpmv2-part1 "Trusted Platform Module Library, Part 1: Architecture") section 31.2, i.e., `qualifiedSigner`, `clockInfo` and `firmwareVersion` are ignored. Depending on the properties of the aikCert key used, these fields may be obfuscated. If valid, these MAY be used as an input to risk engines.
- If successful, return implementation-specific values representing [attestation type](#attestation-type) [AttCA](#attca) and [attestation trust path](#attestation-trust-path) x5c.

#### 8.3.1. TPM Attestation Statement Certificate Requirements

TPM [attestation certificate](#attestation-certificate) MUST have the following fields/extensions:

- Version MUST be set to 3.
- Subject field MUST be set to empty.
- The Subject Alternative Name extension MUST be set as defined in [\[TPMv2-EK-Profile\]](#biblio-tpmv2-ek-profile "TCG EK Credential Profile for TPM Family 2.0") section 3.2.9.
	Note: Previous versions of [\[TPMv2-EK-Profile\]](#biblio-tpmv2-ek-profile "TCG EK Credential Profile for TPM Family 2.0") allowed the inclusion of an optional attribute, called HardwareModuleName, that contains the TPM serial number in the EK certificate. HardwareModuleName SHOULD NOT be placed in in the [attestation certificate](#attestation-certificate) Subject Alternative Name.
- The Extended Key Usage extension MUST contain the OID `2.23.133.8.3` ("joint-iso-itu-t(2) internationalorganizations(23) 133 tcg-kp(8) tcg-kp-AIKCertificate(3)").
- The Basic Constraints extension MUST have the CA component set to `false`.
- An Authority Information Access (AIA) extension with entry `id-ad-ocsp` and a CRL Distribution Point extension [\[RFC5280\]](#biblio-rfc5280 "Internet X.509 Public Key Infrastructure Certificate and Certificate Revocation List (CRL) Profile") are both OPTIONAL as the status of many attestation certificates is available through metadata services. See, for example, the FIDO Metadata Service [\[FIDOMetadataService\]](#biblio-fidometadataservice "FIDO Metadata Service").

### 8.4. Android Key Attestation Statement Format

When the [authenticator](#authenticator) in question is a [platform authenticator](#platform-authenticators) on the Android "N" or later platform, the attestation statement is based on the [Android key attestation](https://source.android.com/security/keystore/attestation). In these cases, the attestation statement is produced by a component running in a secure operating environment, but the [authenticator data for the attestation](#authenticator-data-for-the-attestation) is produced outside this environment. The [WebAuthn Relying Party](#webauthn-relying-party) is expected to check that the [authenticator data claimed to have been used for the attestation](#authenticator-data-claimed-to-have-been-used-for-the-attestation) is consistent with the fields of the attestation certificate’s extension data.

Attestation statement format identifier

android-key

Attestation types supported

[Basic](#basic)

Syntax

An Android key attestation statement consists simply of the Android attestation statement, which is a series of DER encoded X.509 certificates. See [the Android developer documentation](https://developer.android.com/training/articles/security-key-attestation.html). Its syntax is defined as follows:

```
$$attStmtType //= (
                      fmt: "android-key",
                      attStmt: androidStmtFormat
                  )

androidStmtFormat = {
                      alg: COSEAlgorithmIdentifier,
                      sig: bytes,
                      x5c: [ credCert: bytes, * (caCert: bytes) ]
                    }
```

Signing procedure

Let authenticatorData denote the [authenticator data for the attestation](#authenticator-data-for-the-attestation), and let clientDataHash denote the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data).

Request an Android Key Attestation by calling `keyStore.getCertificateChain(myKeyUUID)` providing clientDataHash as the challenge value (e.g., by using [setAttestationChallenge](https://developer.android.com/reference/android/security/keystore/KeyGenParameterSpec.Builder.html#setAttestationChallenge\(byte%5B%5D\))). Set x5c to the returned value.

The authenticator produces sig by concatenating authenticatorData and clientDataHash, and signing the result using the credential private key. It sets alg to the algorithm of the signature format.

Verification procedure

Given the [verification procedure inputs](#verification-procedure-inputs) attStmt, authenticatorData and clientDataHash, the [verification procedure](#verification-procedure) is as follows:

- Verify that attStmt is valid CBOR conforming to the syntax defined above and perform CBOR decoding on it to extract the contained fields.
- Verify that sig is a valid signature over the concatenation of authenticatorData and clientDataHash using the public key in the first certificate in x5c with the algorithm specified in alg.
- Verify that the public key in the first certificate in x5c matches the `credentialPublicKey` in the `attestedCredentialData` in authenticatorData.
- Verify that the `attestationChallenge` field in the [attestation certificate](#attestation-certificate) [extension data](#android-key-attestation-certificate-extension-data) is identical to clientDataHash.
- Verify the following using the appropriate authorization list from the attestation certificate [extension data](#android-key-attestation-certificate-extension-data):
	- The `AuthorizationList.allApplications` field is *not* present on either authorization list (`softwareEnforced` nor `teeEnforced`), since PublicKeyCredential MUST be [scoped](#scope) to the [RP ID](#rp-id).
		- For the following, use only the `teeEnforced` authorization list if the RP wants to accept only keys from a trusted execution environment, otherwise use the union of `teeEnforced` and `softwareEnforced`.
		- The value in the `AuthorizationList.origin` field is equal to `KM_ORIGIN_GENERATED`.
				- The value in the `AuthorizationList.purpose` field is equal to `KM_PURPOSE_SIGN`.
- If successful, return implementation-specific values representing [attestation type](#attestation-type) [Basic](#basic) and [attestation trust path](#attestation-trust-path) x5c.

#### 8.4.1. Android Key Attestation Statement Certificate Requirements

Android Key Attestation [attestation certificate](#attestation-certificate) ’s android key attestation certificate extension data is identified by the OID `1.3.6.1.4.1.11129.2.1.17`, and its schema is defined in the [Android developer documentation](https://developer.android.com/training/articles/security-key-attestation#certificate_schema).

### 8.5. Android SafetyNet Attestation Statement Format

Note: This format is deprecated and is expected to be removed in a future revision of this document.

When the [authenticator](#authenticator) is a [platform authenticator](#platform-authenticators) on certain Android platforms, the attestation statement may be based on the [SafetyNet API](https://developer.android.com/training/safetynet/attestation#compat-check-response). In this case the [authenticator data](#authenticator-data) is completely controlled by the caller of the SafetyNet API (typically an application running on the Android platform) and the attestation statement provides some statements about the health of the platform and the identity of the calling application (see [SafetyNet Documentation](https://developer.android.com/training/safetynet/attestation.html) for more details).

Attestation statement format identifier

android-safetynet

Attestation types supported

[Basic](#basic)

Syntax

The syntax of an Android Attestation statement is defined as follows:

```
$$attStmtType //= (
                      fmt: "android-safetynet",
                      attStmt: safetynetStmtFormat
                  )

safetynetStmtFormat = {
                          ver: text,
                          response: bytes
                      }
```

The semantics of the above fields are as follows:

ver

The version number of Google Play Services responsible for providing the SafetyNet API.

response

The [UTF-8 encoded](https://encoding.spec.whatwg.org/#utf-8-encode) result of the getJwsResult() call of the SafetyNet API. This value is a JWS [\[RFC7515\]](#biblio-rfc7515 "JSON Web Signature (JWS)") object (see [SafetyNet online documentation](https://developer.android.com/training/safetynet/attestation#compat-check-response)) in Compact Serialization.

Signing procedure

Let authenticatorData denote the [authenticator data for the attestation](#authenticator-data-for-the-attestation), and let clientDataHash denote the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data).

Concatenate authenticatorData and clientDataHash, perform SHA-256 hash of the concatenated string, and let the result of the hash form attToBeSigned.

Request a SafetyNet attestation, providing attToBeSigned as the nonce value. Set response to the result, and ver to the version of Google Play Services running in the authenticator.

Verification procedure

Given the [verification procedure inputs](#verification-procedure-inputs) attStmt, authenticatorData and clientDataHash, the [verification procedure](#verification-procedure) is as follows:

- Verify that attStmt is valid CBOR conforming to the syntax defined above and perform CBOR decoding on it to extract the contained fields.
- Verify that response is a valid SafetyNet response of version ver by following the steps indicated by the [SafetyNet online documentation](https://developer.android.com/training/safetynet/attestation.html#compat-check-response). As of this writing, there is only one format of the SafetyNet response and ver is reserved for future use.
- Verify that the `nonce` attribute in the payload of response is identical to the Base64 encoding of the SHA-256 hash of the concatenation of authenticatorData and clientDataHash.
- Verify that the SafetyNet response actually came from the SafetyNet service by following the steps in the [SafetyNet online documentation](https://developer.android.com/training/safetynet/attestation#compat-check-response).
- If successful, return implementation-specific values representing [attestation type](#attestation-type) [Basic](#basic) and [attestation trust path](#attestation-trust-path) x5c.

### 8.6. FIDO U2F Attestation Statement Format

This attestation statement format is used with FIDO U2F authenticators using the formats defined in [\[FIDO-U2F-Message-Formats\]](#biblio-fido-u2f-message-formats "FIDO U2F Raw Message Formats").

Attestation statement format identifier

fido-u2f

Attestation types supported

[Basic](#basic), [AttCA](#attca)

Syntax

The syntax of a FIDO U2F attestation statement is defined as follows:

```
$$attStmtType //= (
                      fmt: "fido-u2f",
                      attStmt: u2fStmtFormat
                  )

u2fStmtFormat = {
                    x5c: [ attestnCert: bytes ],
                    sig: bytes
                }
```

The semantics of the above fields are as follows:

x5c

A single element array containing the attestation certificate in X.509 format.

sig

The [attestation signature](#attestation-signature). The signature was calculated over the (raw) U2F registration response message [\[FIDO-U2F-Message-Formats\]](#biblio-fido-u2f-message-formats "FIDO U2F Raw Message Formats") received by the [client](#client) from the authenticator.

Signing procedure

If the [credential public key](#credential-public-key) of the [attested credential](#authdata-attestedcredentialdata) is not of algorithm -7 ("ES256"), stop and return an error. Otherwise, let authenticatorData denote the [authenticator data for the attestation](#authenticator-data-for-the-attestation), and let clientDataHash denote the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data). (Since SHA-256 is used to hash the serialized [client data](#client-data), clientDataHash will be 32 bytes long.)

Generate a Registration Response Message as specified in [\[FIDO-U2F-Message-Formats\]](#biblio-fido-u2f-message-formats "FIDO U2F Raw Message Formats") [Section 4.3](https://fidoalliance.org/specs/fido-u2f-v1.1-id-20160915/fido-u2f-raw-message-formats-v1.1-id-20160915.html#registration-response-message-success), with the application parameter set to the SHA-256 hash of the [RP ID](#rp-id) that the given [credential](#public-key-credential) is [scoped](#scope) to, the challenge parameter set to clientDataHash, and the key handle parameter set to the [credential ID](#credential-id) of the given credential. Set the raw signature part of this Registration Response Message (i.e., without the [user public key](#user-public-key), key handle, and attestation certificates) as sig and set the attestation certificates of the attestation public key as x5c.

Verification procedure

Given the [verification procedure inputs](#verification-procedure-inputs) attStmt, authenticatorData and clientDataHash, the [verification procedure](#verification-procedure) is as follows:

1. Verify that attStmt is valid CBOR conforming to the syntax defined above and perform CBOR decoding on it to extract the contained fields.
2. Check that x5c has exactly one element and let attCert be that element. Let certificate public key be the public key conveyed by attCert. If certificate public key is not an Elliptic Curve (EC) public key over the P-256 curve, terminate this algorithm and return an appropriate error.
3. Extract the claimed rpIdHash from authenticatorData, and the claimed credentialId and credentialPublicKey from authenticatorData.`attestedCredentialData`.
4. Convert the COSE\_KEY formatted credentialPublicKey (see [Section 7](https://tools.ietf.org/html/rfc9052#section-7) of [\[RFC9052\]](#biblio-rfc9052 "CBOR Object Signing and Encryption (COSE): Structures and Process")) to Raw ANSI X9.62 public key format (see ALG\_KEY\_ECC\_X962\_RAW in [Section 3.6.2 Public Key Representation Formats](https://fidoalliance.org/specs/common-specs/fido-registry-v2.1-ps-20191217.html#public-key-representation-formats) of [\[FIDO-Registry\]](#biblio-fido-registry "FIDO Registry of Predefined Values")).
	- Let x be the value corresponding to the "-2" key (representing x coordinate) in credentialPublicKey, and confirm its size to be of 32 bytes. If size differs or "-2" key is not found, terminate this algorithm and return an appropriate error.
		- Let y be the value corresponding to the "-3" key (representing y coordinate) in credentialPublicKey, and confirm its size to be of 32 bytes. If size differs or "-3" key is not found, terminate this algorithm and return an appropriate error.
		- Let publicKeyU2F be the concatenation `0x04 || x || y`.
		Note: This signifies uncompressed ECC key format.
5. Let verificationData be the concatenation of (0x00 || rpIdHash || clientDataHash || credentialId || publicKeyU2F) (see [Section 4.3](https://fidoalliance.org/specs/fido-u2f-v1.1-id-20160915/fido-u2f-raw-message-formats-v1.1-id-20160915.html#registration-response-message-success) of [\[FIDO-U2F-Message-Formats\]](#biblio-fido-u2f-message-formats "FIDO U2F Raw Message Formats")).
6. Verify the sig using verificationData and the certificate public key per section 4.1.4 of [\[SEC1\]](#biblio-sec1 "SEC1: Elliptic Curve Cryptography, Version 2.0") with SHA-256 as the hash function used in step two.
7. Optionally, inspect x5c and consult externally provided knowledge to determine whether attStmt conveys a [Basic](#basic) or [AttCA](#attca) attestation.
8. If successful, return implementation-specific values representing [attestation type](#attestation-type) [Basic](#basic), [AttCA](#attca) or uncertainty, and [attestation trust path](#attestation-trust-path) x5c.

### 8.7. None Attestation Statement Format

The none attestation statement format is used to replace any [authenticator](#authenticator) -provided [attestation statement](#attestation-statement) when a [WebAuthn Relying Party](#webauthn-relying-party) indicates it does not wish to receive attestation information, see [§ 5.4.7 Attestation Conveyance Preference Enumeration (enum AttestationConveyancePreference)](#enum-attestation-convey).

The [authenticator](#authenticator) MAY also directly generate attestation statements of this format if the [authenticator](#authenticator) does not support [attestation](#attestation).

Attestation statement format identifier

none

Attestation types supported

[None](#none)

Syntax

The syntax of a none attestation statement is defined as follows:

```
$$attStmtType //= (
                      fmt: "none",
                      attStmt: emptyMap
                  )

emptyMap = {}
```

Signing procedure

Return the fixed attestation statement defined above.

Verification procedure

Return implementation-specific values representing [attestation type](#attestation-type) [None](#none) and an empty [attestation trust path](#attestation-trust-path).

### 8.8. Apple Anonymous Attestation Statement Format

This attestation statement format is exclusively used by Apple for certain types of Apple devices that support WebAuthn.

Attestation statement format identifier

apple

Attestation types supported

[Anonymization CA](#anonymization-ca)

Syntax

The syntax of an Apple attestation statement is defined as follows:

```
$$attStmtType //= (
                      fmt: "apple",
                      attStmt: appleStmtFormat
                  )

appleStmtFormat = {
                      x5c: [ credCert: bytes, * (caCert: bytes) ]
                  }
```

The semantics of the above fields are as follows:

x5c

credCert followed by its certificate chain, each encoded in X.509 format.

credCert

The credential public key certificate used for attestation, encoded in X.509 format.

Signing procedure

1. Let authenticatorData denote the authenticator data for the attestation, and let clientDataHash denote the [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data).
2. Concatenate authenticatorData and clientDataHash to form nonceToHash.
3. Perform SHA-256 hash of nonceToHash to produce nonce.
4. Let Apple anonymous attestation CA generate an X.509 certificate for the [credential public key](#credential-public-key) and include the nonce as a certificate extension with OID `1.2.840.113635.100.8.2`. credCert denotes this certificate. The credCert thus serves as a proof of the attestation, and the included nonce proves the attestation is live. In addition to that, the nonce also protects the integrity of the authenticatorData and [client data](#client-data).
5. Set x5c to credCert followed by its certificate chain.

Verification procedure

Given the verification procedure inputs attStmt, authenticatorData and clientDataHash, the verification procedure is as follows:

1. Verify that attStmt is valid CBOR conforming to the syntax defined above and perform CBOR decoding on it to extract the contained fields.
2. Concatenate authenticatorData and clientDataHash to form nonceToHash.
3. Perform SHA-256 hash of nonceToHash to produce nonce.
4. Verify that nonce equals the value of the extension with OID `1.2.840.113635.100.8.2` in credCert.
5. Verify that the [credential public key](#credential-public-key) equals the Subject Public Key of credCert.
6. If successful, return implementation-specific values representing attestation type [Anonymization CA](#anonymization-ca) and attestation trust path x5c.

### 8.9. Compound Attestation Statement Format

The "compound" attestation statement format is used to pass multiple, self-contained attestation statements in a single ceremony.

Attestation statement format identifier

compound

Attestation types supported

Any. See [§ 6.5.3 Attestation Types](#sctn-attestation-types).

Syntax

The syntax of a compound attestation statement is defined as follows:

```
$$attStmtType //= (
                      fmt: "compound",
                      attStmt: [2* nonCompoundAttStmt]
                  )

nonCompoundAttStmt = { $$attStmtType } .within { fmt: text .ne "compound", * any => any }
```

Signing procedure

Not applicable

Verification procedure

Given the [verification procedure inputs](#verification-procedure-inputs) attStmt, authenticatorData and clientDataHash, the [verification procedure](#verification-procedure) is as follows:

1. [For each](https://infra.spec.whatwg.org/#list-iterate) subStmt of attStmt, evaluate the [verification procedure](#verification-procedure) corresponding to the [attestation statement format identifier](#attestation-statement-format-identifier) `subStmt.fmt` with [verification procedure inputs](#verification-procedure-inputs) subStmt, authenticatorData and clientDataHash.
	If validation fails for one or more subStmt, decide the appropriate result based on [Relying Party](#relying-party) policy.
2. If sufficiently many (as determined by [Relying Party](#relying-party) policy) [items](https://infra.spec.whatwg.org/#list-item) of attStmt verify successfully, return implementation-specific values representing any combination of outputs from successful [verification procedures](#verification-procedure).

## 9\. WebAuthn Extensions

The mechanism for generating [public key credentials](#public-key-credential), as well as requesting and generating Authentication assertions, as defined in [§ 5 Web Authentication API](#sctn-api), can be extended to suit particular use cases. Each case is addressed by defining a registration extension and/or an authentication extension.

Every extension is a client extension, meaning that the extension involves communication with and processing by the client. [Client extensions](#client-extension) define the following steps and data:

- `navigator.credentials.create()` extension request parameters and response values for [registration extensions](#registration-extension).
- `navigator.credentials.get()` extension request parameters and response values for [authentication extensions](#authentication-extension).
- [Client extension processing](#client-extension-processing) for [registration extensions](#registration-extension) and [authentication extensions](#authentication-extension).

When creating a [public key credential](#public-key-credential) or requesting an [authentication assertion](#authentication-assertion), a [WebAuthn Relying Party](#webauthn-relying-party) can request the use of a set of extensions. These extensions will be invoked during the requested operation if they are supported by the client and/or the [WebAuthn Authenticator](#webauthn-authenticator). The [Relying Party](#relying-party) sends the [client extension input](#client-extension-input) for each extension in the `get()` call (for [authentication extensions](#authentication-extension)) or `create()` call (for [registration extensions](#registration-extension)) to the [client](#client). The [client](#client) performs [client extension processing](#client-extension-processing) for each extension that the [client platform](#client-platform) supports, and augments the [client data](#client-data) as specified by each extension, by including the [extension identifier](#extension-identifier) and [client extension output](#client-extension-output) values.

An extension can also be an authenticator extension, meaning that the extension involves communication with and processing by the authenticator. [Authenticator extensions](#authenticator-extension) define the following steps and data:

- [authenticatorMakeCredential](#authenticatormakecredential) extension request parameters and response values for [registration extensions](#registration-extension).
- [authenticatorGetAssertion](#authenticatorgetassertion) extension request parameters and response values for [authentication extensions](#authentication-extension).
- [Authenticator extension processing](#authenticator-extension-processing) for [registration extensions](#registration-extension) and [authentication extensions](#authentication-extension).

For [authenticator extensions](#authenticator-extension), as part of the [client extension processing](#client-extension-processing), the client also creates the [CBOR](#cbor) [authenticator extension input](#authenticator-extension-input) value for each extension (often based on the corresponding [client extension input](#client-extension-input) value), and passes them to the authenticator in the `create()` call (for [registration extensions](#registration-extension)) or the `get()` call (for [authentication extensions](#authentication-extension)). These [authenticator extension input](#authenticator-extension-input) values are represented in [CBOR](#cbor) and passed as name-value pairs, with the [extension identifier](#extension-identifier) as the name, and the corresponding [authenticator extension input](#authenticator-extension-input) as the value. The authenticator, in turn, performs additional processing for the extensions that it supports, and returns the [CBOR](#cbor) [authenticator extension output](#authenticator-extension-output) for each as specified by the extension. Since [authenticator extension output](#authenticator-extension-output) is returned as part of the signed [authenticator data](#authenticator-data), authenticator extensions MAY also specify an [unsigned extension output](#unsigned-extension-outputs), e.g. for cases where an output itself depends on [authenticator data](#authenticator-data). Part of the [client extension processing](#client-extension-processing) for [authenticator extensions](#authenticator-extension) is to use the [authenticator extension output](#authenticator-extension-output) and [unsigned extension output](#unsigned-extension-outputs) as an input to creating the [client extension output](#client-extension-output).

All [WebAuthn Extensions](#webauthn-extensions) are OPTIONAL for both clients and authenticators. Thus, any extensions requested by a [Relying Party](#relying-party) MAY be ignored by the client browser or OS and not passed to the authenticator at all, or they MAY be ignored by the authenticator. Ignoring an extension is never considered a failure in WebAuthn API processing, so when [Relying Parties](#relying-party) include extensions with any API calls, they MUST be prepared to handle cases where some or all of those extensions are ignored.

All [WebAuthn Extensions](#webauthn-extensions) MUST be defined in such a way that lack of support for them by the [client](#client) or [authenticator](#authenticator) does not endanger the user’s security or privacy. For instance, if an extension requires client processing, it could be defined in a manner that ensures that a naïve pass-through that simply transcodes [client extension inputs](#client-extension-input) from JSON to CBOR will produce a semantically invalid [authenticator extension input](#authenticator-extension-input) value, resulting in the extension being ignored by the authenticator. Since all extensions are OPTIONAL, this will not cause a functional failure in the API operation.

The IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)") can be consulted for an up-to-date list of registered [WebAuthn Extensions](#webauthn-extensions).

### 9.1. Extension Identifiers

Extensions are identified by a string, called an extension identifier, chosen by the extension author.

Extension identifiers SHOULD be registered in the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)"). All registered extension identifiers are unique amongst themselves as a matter of course.

Unregistered extension identifiers SHOULD aim to be globally unique, e.g., by including the defining entity such as `myCompany_extension`.

All extension identifiers MUST be a maximum of 32 octets in length and MUST consist only of printable USASCII characters, excluding backslash and doublequote, i.e., VCHAR as defined in [\[RFC5234\]](#biblio-rfc5234 "Augmented BNF for Syntax Specifications: ABNF") but without %x22 and %x5c. Implementations MUST match WebAuthn extension identifiers in a case-sensitive fashion.

Extensions that may exist in multiple versions should take care to include a version in their identifier. In effect, different versions are thus treated as different extensions, e.g., `myCompany_extension_01`

[§ 10 Defined Extensions](#sctn-defined-extensions) defines an additional set of extensions and their identifiers. See the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)") for an up-to-date list of registered WebAuthn Extension Identifiers.

### 9.2. Defining Extensions

A definition of an extension MUST specify an [extension identifier](#extension-identifier), a [client extension input](#client-extension-input) argument to be sent via the `get()` or `create()` call, the [client extension processing](#client-extension-processing) rules, and a [client extension output](#client-extension-output) value. If the extension communicates with the authenticator (meaning it is an [authenticator extension](#authenticator-extension)), it MUST also specify the [CBOR](#cbor) [authenticator extension input](#authenticator-extension-input) argument sent via the [authenticatorGetAssertion](#authenticatorgetassertion) or [authenticatorMakeCredential](#authenticatormakecredential) call, the [authenticator extension processing](#authenticator-extension-processing) rules, and the [CBOR](#cbor) [authenticator extension output](#authenticator-extension-output) value. Extensions MAY specify [unsigned extension outputs](#unsigned-extension-outputs).

Any [client extension](#client-extension) that is processed by the client MUST return a [client extension output](#client-extension-output) value so that the [WebAuthn Relying Party](#webauthn-relying-party) knows that the extension was honored by the client. Similarly, any extension that requires authenticator processing MUST return an [authenticator extension output](#authenticator-extension-output) to let the [Relying Party](#relying-party) know that the extension was honored by the authenticator. If an extension does not otherwise require any result values, it SHOULD be defined as returning a JSON Boolean [client extension output](#client-extension-output) result, set to `true` to signify that the extension was understood and processed. Likewise, any [authenticator extension](#authenticator-extension) that does not otherwise require any result values MUST return a value and SHOULD return a CBOR Boolean [authenticator extension output](#authenticator-extension-output) result, set to `true` to signify that the extension was understood and processed.

### 9.3. Extending Request Parameters

An extension defines one or two request arguments. The client extension input, which is a value that can be encoded in JSON, is passed from the [WebAuthn Relying Party](#webauthn-relying-party) to the client in the `get()` or `create()` call, while the [CBOR](#cbor) authenticator extension input is passed from the client to the authenticator for [authenticator extensions](#authenticator-extension) during the processing of these calls.

A [Relying Party](#relying-party) simultaneously requests the use of an extension and sets its [client extension input](#client-extension-input) by including an entry in the `extensions` option to the `create()` or `get()` call. The entry key is the [extension identifier](#extension-identifier) and the value is the [client extension input](#client-extension-input).

Note: Other documents have specified extensions where the extension input does not always use the [extension identifier](#extension-identifier) as the entry key. The above convention still applies to new extensions.

```
var assertionPromise = navigator.credentials.get({
    publicKey: {
        // Other members omitted for brevity
        extensions: {
            // An "entry key" identifying the "webauthnExample_foobar" extension,
            // whose value is a map with two input parameters:
            "webauthnExample_foobar": {
              foo: 42,
              bar: "barfoo"
            }
        }
    }
});
```

Extension definitions MUST specify the valid values for their [client extension input](#client-extension-input). Clients SHOULD ignore extensions with an invalid [client extension input](#client-extension-input). If an extension does not require any parameters from the [Relying Party](#relying-party), it SHOULD be defined as taking a Boolean client argument, set to `true` to signify that the extension is requested by the [Relying Party](#relying-party).

Extensions that only affect client processing need not specify [authenticator extension input](#authenticator-extension-input). Extensions that have authenticator processing MUST specify the method of computing the [authenticator extension input](#authenticator-extension-input) from the [client extension input](#client-extension-input), and MUST define extensions for the [CDDL](#cddl) types `AuthenticationExtensionsAuthenticatorInputs` and `AuthenticationExtensionsAuthenticatorOutputs` by defining an additional choice for the `$$extensionInput` and `$$extensionOutput` [group sockets](https://tools.ietf.org/html/rfc8610#section-3.9), and OPTIONALLY the `$$unsignedExtensionOutput` [group socket](https://tools.ietf.org/html/rfc8610#section-3.9), using the [extension identifier](#extension-identifier) as the entry key. Extensions that do not require input parameters, and are thus defined as taking a Boolean [client extension input](#client-extension-input) value set to `true`, SHOULD define the [authenticator extension input](#authenticator-extension-input) also as the constant Boolean value `true` (CBOR major type 7, value 21).

The following example defines that an extension with [identifier](#extension-identifier) `webauthnExample_foobar` takes an unsigned integer as [authenticator extension input](#authenticator-extension-input), and returns an array of at least one byte string as [authenticator extension output](#authenticator-extension-output), with no [unsigned extension outputs](#unsigned-extension-outputs):

```
$$extensionInput //= (
  webauthnExample_foobar: uint
)
$$extensionOutput //= (
  webauthnExample_foobar: [+ bytes]
)
```

Because some authenticators communicate over low-bandwidth links such as Bluetooth Low-Energy or NFC, extensions SHOULD aim to define authenticator arguments that are as small as possible.

### 9.4. Client Extension Processing

Extensions MAY define additional processing requirements on the [client](#client) during the creation of credentials or the generation of an assertion. The [client extension input](#client-extension-input) for the extension is used as an input to this client processing. For each supported [client extension](#client-extension), the client adds an entry to the clientExtensions [map](https://infra.spec.whatwg.org/#ordered-map) with the [extension identifier](#extension-identifier) as the key, and the extension’s [client extension input](#client-extension-input) as the value.

Likewise, the [client extension outputs](#client-extension-output) are represented as a dictionary in the result of `getClientExtensionResults()` with [extension identifiers](#extension-identifier) as keys, and the client extension output value of each extension as the value. Like the [client extension input](#client-extension-input), the [client extension output](#client-extension-output) is a value that can be encoded in JSON. There MUST NOT be any values returned for ignored extensions.

Extensions that require authenticator processing MUST define the process by which the [client extension input](#client-extension-input) can be used to determine the [CBOR](#cbor) [authenticator extension input](#authenticator-extension-input) and the process by which the [CBOR](#cbor) [authenticator extension output](#authenticator-extension-output), and the [unsigned extension output](#unsigned-extension-outputs) if used, can be used to determine the [client extension output](#client-extension-output).

### 9.5. Authenticator Extension Processing

The [CBOR](#cbor) [authenticator extension input](#authenticator-extension-input) value of each processed [authenticator extension](#authenticator-extension) is included in the extensions parameter of the [authenticatorMakeCredential](#authenticatormakecredential) and [authenticatorGetAssertion](#authenticatorgetassertion) operations. The extensions parameter is a [CBOR](#cbor) map where each key is an [extension identifier](#extension-identifier) and the corresponding value is the [authenticator extension input](#authenticator-extension-input) for that extension.

Likewise, the extension output is represented in the [extensions](#authdata-extensions) part of the [authenticator data](#authenticator-data). The [extensions](#authdata-extensions) part of the [authenticator data](#authenticator-data) is a CBOR map where each key is an [extension identifier](#extension-identifier) and the corresponding value is the authenticator extension output for that extension.

Unsigned extension outputs are represented independently from [authenticator data](#authenticator-data) and returned by authenticators as a separate map, keyed with the same [extension identifier](#extension-identifier). This map only contains entries for authenticator extensions that make use of unsigned outputs. Unsigned outputs are useful when extensions output a signature over the [authenticator data](#authenticator-data) (because otherwise a signature would have to sign over itself, which isn’t possible) or when some extension outputs should not be sent to the [Relying Party](#relying-party).

Note: In [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") [unsigned extension outputs](#unsigned-extension-outputs) are returned as a CBOR map in a top-level field named `unsignedExtensionOutputs` from both [authenticatorMakeCredential](#authenticatormakecredential) and [authenticatorGetAssertion](#authenticatorgetassertion).

For each supported extension, the [authenticator extension processing](#authenticator-extension-processing) rule for that extension is used create the [authenticator extension output](#authenticator-extension-output), and [unsigned extension output](#unsigned-extension-outputs) if used, from the [authenticator extension input](#authenticator-extension-input) and possibly also other inputs. There MUST NOT be any values returned for ignored extensions.

## 10\. Defined Extensions

This section and its subsections define an additional set of extensions to be registered in the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)"). These MAY be implemented by user agents targeting broad interoperability.

### 10.1. Client Extensions

This section defines extensions that are only [client extensions](#client-extension).

#### 10.1.1. FIDO AppID Extension (appid)

This extension allows [WebAuthn Relying Parties](#webauthn-relying-party) that have previously registered a credential using the legacy FIDO U2F JavaScript API [\[FIDOU2FJavaScriptAPI\]](#biblio-fidou2fjavascriptapi "FIDO U2F JavaScript API") to request an [assertion](#assertion). The FIDO APIs use an alternative identifier for [Relying Parties](#relying-party) called an AppID [\[FIDO-APPID\]](#biblio-fido-appid "FIDO AppID and Facet Specification"), and any credentials created using those APIs will be [scoped](#scope) to that identifier. Without this extension, they would need to be re-registered in order to be [scoped](#scope) to an [RP ID](#rp-id).

In addition to setting the `appid` extension input, using this extension requires some additional processing by the [Relying Party](#relying-party) in order to allow users to [authenticate](#authentication) using their registered U2F credentials:

1. List the desired U2F credentials in the `allowCredentials` option of the `get()` method:
	- Set the `type` members to `public-key`.
		- Set the `id` members to the respective U2F key handles of the desired credentials. Note that U2F key handles commonly use [base64url encoding](#base64url-encoding) but must be decoded to their binary form when used in `id`.
	`allowCredentials` MAY contain a mixture of both WebAuthn [credential IDs](#credential-id) and U2F key handles; stating the `appid` via this extension does not prevent the user from using a WebAuthn-registered credential scoped to the [RP ID](#rp-id) stated in `rpId`.
2. When [verifying the assertion](#rp-op-verifying-assertion-step-rpid-hash), expect that the `rpIdHash` MAY be the hash of the AppID instead of the [RP ID](#rp-id).

This extension does not allow FIDO-compatible credentials to be created. Thus, credentials created with WebAuthn are not backwards compatible with the FIDO JavaScript APIs.

Note: `appid` should be set to the AppID that the [Relying Party](#relying-party) *previously* used in the legacy FIDO APIs. This might not be the same as the result of translating the [Relying Party](#relying-party) ’s WebAuthn [RP ID](#rp-id) to the AppID format, e.g., the previously used AppID may have been "https://accounts.example.com" but the currently used [RP ID](#rp-id) might be "example.com".

Extension identifier

`appid`

Operation applicability

[Authentication](#authentication-extension)

Client extension input

A single DOMString specifying a FIDO AppID.

```
partial dictionary AuthenticationExtensionsClientInputs {
  DOMString appid;
};
partial dictionary AuthenticationExtensionsClientInputsJSON {
  DOMString appid;
};
```

Client extension processing

1. Let facetId be the result of passing the caller’s [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) to the FIDO algorithm for [determining the FacetID of a calling application](https://fidoalliance.org/specs/fido-v2.0-id-20180227/fido-appid-and-facets-v2.0-id-20180227.html#determining-the-facetid-of-a-calling-application).
2. Let appId be the extension input.
3. Pass facetId and appId to the FIDO algorithm for [determining if a caller’s FacetID is authorized for an AppID](https://fidoalliance.org/specs/fido-v2.0-id-20180227/fido-appid-and-facets-v2.0-id-20180227.html#determining-if-a-caller-s-facetid-is-authorized-for-an-appid). If that algorithm rejects appId then return a " `SecurityError` " `DOMException`.
4. When [building allowCredentialDescriptorList](#allowCredentialDescriptorListCreation), if a U2F authenticator indicates that a credential is inapplicable (i.e. by returning `SW_WRONG_DATA`) then the client MUST retry with the U2F application parameter set to the SHA-256 hash of appId. If this results in an applicable credential, the client MUST include the credential in allowCredentialDescriptorList. The value of appId then replaces the `rpId` parameter of [authenticatorGetAssertion](#authenticatorgetassertion).
5. Let output be the Boolean value `false`.
6. When [creating assertionCreationData](#assertionCreationDataCreation), if the [assertion](#assertion) was created by a U2F authenticator with the U2F application parameter set to the SHA-256 hash of appId instead of the SHA-256 hash of the [RP ID](#rp-id), set output to `true`.

Note: In practice, several implementations do not implement steps four and onward of the algorithm for [determining if a caller’s FacetID is authorized for an AppID](https://fidoalliance.org/specs/fido-v2.0-id-20180227/fido-appid-and-facets-v2.0-id-20180227.html#determining-if-a-caller-s-facetid-is-authorized-for-an-appid). Instead, in step three, the comparison on the host is relaxed to accept hosts on the [same site](https://html.spec.whatwg.org/multipage/browsers.html#same-site).

Client extension output

Returns the value of output. If true, the AppID was used and thus, when [verifying the assertion](#rp-op-verifying-assertion-step-rpid-hash), the [Relying Party](#relying-party) MUST expect the `rpIdHash` to be the hash of the AppID, not the [RP ID](#rp-id).

```
partial dictionary AuthenticationExtensionsClientOutputs {
  boolean appid;
};
partial dictionary AuthenticationExtensionsClientOutputsJSON {
  boolean appid;
};
```

Authenticator extension input

None.

Authenticator extension processing

None.

Authenticator extension output

None.

#### 10.1.2. FIDO AppID Exclusion Extension (appidExclude)

This registration extension allows [WebAuthn Relying Parties](#webauthn-relying-party) to exclude authenticators that contain specified credentials that were created with the legacy FIDO U2F JavaScript API [\[FIDOU2FJavaScriptAPI\]](#biblio-fidou2fjavascriptapi "FIDO U2F JavaScript API").

During a transition from the FIDO U2F JavaScript API, a [Relying Party](#relying-party) may have a population of users with legacy credentials already registered. The [appid](#sctn-appid-extension) extension allows the sign-in flow to be transitioned smoothly but, when transitioning the registration flow, the [excludeCredentials](#dom-publickeycredentialcreationoptions-excludecredentials) field will not be effective in excluding authenticators with legacy credentials because its contents are taken to be WebAuthn credentials. This extension directs [client platforms](#client-platform) to consider the contents of [excludeCredentials](#dom-publickeycredentialcreationoptions-excludecredentials) as both WebAuthn and legacy FIDO credentials. Note that U2F key handles commonly use [base64url encoding](#base64url-encoding) but must be decoded to their binary form when used in [excludeCredentials](#dom-publickeycredentialcreationoptions-excludecredentials).

Extension identifier

`appidExclude`

Operation applicability

[Registration](#registration-extension)

Client extension input

A single DOMString specifying a FIDO AppID.

```
partial dictionary AuthenticationExtensionsClientInputs {
  DOMString appidExclude;
};
partial dictionary AuthenticationExtensionsClientInputsJSON {
  DOMString appidExclude;
};
```

Client extension processing

When [creating a new credential](#sctn-createCredential):

1. Just after [establishing the RP ID](#CreateCred-DetermineRpId) perform these steps:
	1. Let facetId be the result of passing the caller’s [origin](https://html.spec.whatwg.org/multipage/origin.html#concept-origin) to the FIDO algorithm for [determining the FacetID of a calling application](https://fidoalliance.org/specs/fido-v2.0-id-20180227/fido-appid-and-facets-v2.0-id-20180227.html#determining-the-facetid-of-a-calling-application).
		2. Let appId be the value of the extension input `appidExclude`.
		3. Pass facetId and appId to the FIDO algorithm for [determining if a caller’s FacetID is authorized for an AppID](https://fidoalliance.org/specs/fido-v2.0-id-20180227/fido-appid-and-facets-v2.0-id-20180227.html#determining-if-a-caller-s-facetid-is-authorized-for-an-appid). If the latter algorithm rejects appId then return a " `SecurityError` " `DOMException` and terminate the [creating a new credential](#sctn-createCredential) algorithm as well as these steps.
		Note: In practice, several implementations do not implement steps four and onward of the algorithm for [determining if a caller’s FacetID is authorized for an AppID](https://fidoalliance.org/specs/fido-v2.0-id-20180227/fido-appid-and-facets-v2.0-id-20180227.html#determining-if-a-caller-s-facetid-is-authorized-for-an-appid). Instead, in step three, the comparison on the host is relaxed to accept hosts on the [same site](https://html.spec.whatwg.org/multipage/browsers.html#same-site).
		4. Otherwise, continue with normal processing.
2. Just prior to [invoking authenticatorMakeCredential](#CreateCred-InvokeAuthnrMakeCred) perform these steps:
	1. If authenticator supports the U2F protocol [\[FIDO-U2F-Message-Formats\]](#biblio-fido-u2f-message-formats "FIDO U2F Raw Message Formats"), then [for each](https://infra.spec.whatwg.org/#list-iterate) [credential descriptor](#dictdef-publickeycredentialdescriptor) C in excludeCredentialDescriptorList:
		1. Check whether C was created using U2F on authenticator by sending a `U2F_AUTHENTICATE` message to authenticator whose "five parts" are set to the following values:
			control byte
			`0x07` ("check-only")
			challenge parameter
			32 random bytes
			application parameter
			SHA-256 hash of appId
			key handle length
			The length of `` C.`id` `` (in bytes)
			key handle
			The value of `` C.`id` ``, i.e., the [credential id](#credential-id).
				2. If authenticator responds with `message:error:test-of-user-presence-required` (i.e., success): cease normal processing of this authenticator and indicate in a platform-specific manner that the authenticator is inapplicable. For example, this could be in the form of UI, or could involve requesting from authenticator and, upon receipt, treating it as if the authenticator had returned `InvalidStateError`. Requesting can be accomplished by sending another `U2F_AUTHENTICATE` message to authenticator as above except for setting control byte to `0x03` ("enforce-user-presence-and-sign"), and ignoring the response.
		2. Continue with normal processing.

Client extension output

Returns the value `true` to indicate to the [Relying Party](#relying-party) that the extension was acted upon.

```
partial dictionary AuthenticationExtensionsClientOutputs {
  boolean appidExclude;
};
partial dictionary AuthenticationExtensionsClientOutputsJSON {
  boolean appidExclude;
};
```

Authenticator extension input

None.

Authenticator extension processing

None.

Authenticator extension output

None.

#### 10.1.3. Credential Properties Extension (credProps)

This [client](#client-extension) [registration extension](#registration-extension) facilitates reporting certain [credential properties](#credential-properties) known by the [client](#client) to the requesting [WebAuthn Relying Party](#webauthn-relying-party) upon creation of a [public key credential source](#public-key-credential-source) as a result of a [registration ceremony](#registration-ceremony).

At this time, one [credential property](#credential-properties) is defined: the [client-side discoverable credential property](#credentialpropertiesoutput-client-side-discoverable-credential-property).

Extension identifier

`credProps`

Operation applicability

[Registration](#registration-extension)

Client extension input

The Boolean value `true` to indicate that this extension is requested by the [Relying Party](#relying-party).

```
partial dictionary AuthenticationExtensionsClientInputs {
    boolean credProps;
};
partial dictionary AuthenticationExtensionsClientInputsJSON {
    boolean credProps;
};
```

Client extension processing

Set `rk` to the value of the requireResidentKey parameter that was used in the [invocation](#CreateCred-InvokeAuthnrMakeCred) of the [authenticatorMakeCredential](#authenticatormakecredential) operation.

Client extension output

[Set](https://infra.spec.whatwg.org/#map-set) ``clientExtensionResults["`credProps`"]["rk"]`` to the value of the requireResidentKey parameter that was used in the [invocation](#CreateCred-InvokeAuthnrMakeCred) of the [authenticatorMakeCredential](#authenticatormakecredential) operation.

```
dictionary CredentialPropertiesOutput {
    boolean rk;
};

partial dictionary AuthenticationExtensionsClientOutputs {
    CredentialPropertiesOutput credProps;
};
partial dictionary AuthenticationExtensionsClientOutputsJSON {
    CredentialPropertiesOutput credProps;
};
```

`rk`, of type [boolean](https://webidl.spec.whatwg.org/#idl-boolean)

This OPTIONAL property, known abstractly as the client-side discoverable credential property or as the resident key credential property, is a Boolean value indicating whether the `PublicKeyCredential` returned as a result of a [registration ceremony](#registration-ceremony) is a [client-side discoverable credential](#client-side-discoverable-credential). If `rk` is `true`, the credential is a [discoverable credential](#discoverable-credential). If `rk` is `false`, the credential is a [server-side credential](#server-side-credential). If `rk` is not present, it is not known whether the credential is a [discoverable credential](#discoverable-credential) or a [server-side credential](#server-side-credential).

Note: Some [authenticators](#authenticator) create [discoverable credentials](#discoverable-credential) even when not requested by the [client platform](#client-platform). Because of this, [client platforms](#client-platform) may be forced to omit the `rk` property because they lack the assurance to be able to set it to `false`. [Relying Parties](#relying-party) should assume that, if the `credProps` extension is supported, then [client platforms](#client-platform) will endeavour to populate the `rk` property. Therefore a missing `rk` indicates that the created credential is most likely a [non-discoverable credential](#non-discoverable-credential).

Authenticator extension input

None.

Authenticator extension processing

None.

Authenticator extension output

None.

#### 10.1.4. Pseudo-random function extension (prf)

This [client](#client-extension) [registration extension](#registration-extension) and [authentication extension](#authentication-extension) allows a [Relying Party](#relying-party) to evaluate outputs from a pseudo-random function (PRF) associated with a [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential). The PRFs provided by this extension map from `BufferSource` s of any length to 32-byte `BufferSource` s.

As a motivating example, PRF outputs could be used as symmetric keys to encrypt user data. Such encrypted data would be inaccessible without the ability to get assertions from the associated [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential). By using the provision below to evaluate the PRF at two inputs in a single [assertion](#assertion) operation, the encryption key could be periodically rotated during [assertions](#assertion) by choosing a fresh, random input and reencrypting under the new output. If the evaluation inputs are unpredictable then even an attacker who could satisfy [user verification](#user-verification), and who had time-limited access to the authenticator, could not learn the encryption key without also knowing the correct PRF input.

This extension is modeled on top of the [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") `hmac-secret` extension, but can also be implemented by other means. It is a separate [client extension](#client-extension) because `hmac-secret` requires that inputs and outputs be encrypted in a manner that only the user agent can perform, and to provide separation between uses by WebAuthn and any uses by the underlying platform. This separation is achieved by hashing the provided PRF inputs with a context string to prevent evaluation of the PRFs for arbitrary inputs.

The `hmac-secret` extension provides two PRFs per credential: one which is used for requests where [user verification](#user-verification) is performed and another for all other requests. This extension only exposes a single PRF per credential and, when implementing on top of `hmac-secret`, that PRF MUST be the one used for when [user verification](#user-verification) is performed. This overrides the `UserVerificationRequirement` if necessary.

This extension MAY be implemented for [authenticators](#authenticator) that do not use [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)"). The interface for this between [client](#client) and [authenticator](#authenticator), and the construction of the PRF by the authenticator, is only abstractly specified.

Note: Implementing on top of `hmac-secret` causes [authenticator extension outputs](#authenticator-extension-output) that are not present otherwise. These outputs are encrypted and cannot be used by the [Relying Party](#relying-party), but also cannot be deleted by the client since the [authenticator data](#authenticator-data) is signed.

Extension identifier

`prf`

Operation applicability

[Registration](#registration-extension) and [authentication](#authentication-extension)

Client extension input

```
dictionary AuthenticationExtensionsPRFValues {
    required BufferSource first;
    BufferSource second;
};
dictionary AuthenticationExtensionsPRFValuesJSON {
    required Base64URLString first;
    Base64URLString second;
};

dictionary AuthenticationExtensionsPRFInputs {
    AuthenticationExtensionsPRFValues eval;
    record<DOMString, AuthenticationExtensionsPRFValues> evalByCredential;
};
dictionary AuthenticationExtensionsPRFInputsJSON {
    AuthenticationExtensionsPRFValuesJSON eval;
    record<DOMString, AuthenticationExtensionsPRFValuesJSON> evalByCredential;
};

partial dictionary AuthenticationExtensionsClientInputs {
    AuthenticationExtensionsPRFInputs prf;
};
partial dictionary AuthenticationExtensionsClientInputsJSON {
    AuthenticationExtensionsPRFInputsJSON prf;
};
```

`eval`, of type [AuthenticationExtensionsPRFValues](#dictdef-authenticationextensionsprfvalues)

One or two inputs on which to evaluate PRF. Not all [authenticators](#authenticator) support evaluating the PRFs during credential creation so outputs may, or may not, be provided. If not, then an [assertion](#assertion) is needed in order to obtain the outputs.

`evalByCredential`, of type record< [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString), [AuthenticationExtensionsPRFValues](#dictdef-authenticationextensionsprfvalues) >

A record mapping [base64url encoded](#base64url-encoding) [credential IDs](#credential-id) to PRF inputs to evaluate for that credential. Only applicable during [assertions](#assertion) when `allowCredentials` is not empty.

Client extension processing ([registration](#registration-extension))

1. If `evalByCredential` is present, return a `DOMException` whose name is “ `NotSupportedError` ”.
2. If `eval` is present:
	- Let salt1 be the value of ``SHA-256(UTF8Encode("WebAuthn PRF") || 0x00 || `eval`.`first`)``.
		- If `` `eval`.`second` `` is present, let salt2 be the value of ``SHA-256(UTF8Encode("WebAuthn PRF") || 0x00 || `eval`.`second`)``.
3. If the authenticator supports the CTAP2 `hmac-secret` extension [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)"):
	1. Set `hmac-secret` to `true` in the authenticator extensions input.
		2. If salt1 is defined and a future extension to [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") permits evaluation of the PRF at creation time, configure `hmac-secret` inputs accordingly using the values of salt1 and, if defined, salt2.
		3. Set `enabled` to the value of `hmac-secret` in the authenticator extensions output. If not present, set `enabled` to `false`.
		4. Set `results` to the decrypted PRF result(s), if any.
4. If the authenticator does not support the CTAP2 `hmac-secret` extension [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)"), but does support some other implementation compatible with the abstract authenticator processing defined below:
	1. Set `enabled` to `true`.
		2. If salt1 is defined, use some unspecified mechanism to convey salt1 and, if defined, salt2 to the authenticator as PRF inputs, in that order.
		3. Use some unspecified mechanism to receive the PRF outputs from the authenticator. Set `` `results` `` to the evaluation results, if any.

Client extension processing ([authentication](#authentication-extension))

1. If `evalByCredential` is not empty but `allowCredentials` is empty, return a `DOMException` whose name is “ `NotSupportedError` ”.
2. If any [key](https://infra.spec.whatwg.org/#map-key) in `evalByCredential` is the empty string, or is not a valid [base64url encoding](#base64url-encoding), or does not equal the `id` of some element of `allowCredentials` after performing [base64url decoding](#base64url-encoding), then return a `DOMException` whose name is “ `SyntaxError` ”.
3. Initialize the `prf` extension output to an empty dictionary.
4. Let ev be null, and try to find any applicable PRF input(s):
	1. If `evalByCredential` is present and [contains](https://infra.spec.whatwg.org/#map-exists) an [entry](https://infra.spec.whatwg.org/#map-entry) whose [key](https://infra.spec.whatwg.org/#map-key) is the [base64url encoding](#base64url-encoding) of the [credential ID](#credential-id) that will be returned, let ev be the [value](https://infra.spec.whatwg.org/#map-value) of that entry.
		2. If ev is null and `eval` is present, then let ev be the value of `eval`.
5. If ev is not null:
	1. Let salt1 be the value of ``SHA-256(UTF8Encode("WebAuthn PRF") || 0x00 || ev.`first`)``.
		2. If `` ev.`second` `` is present, let salt2 be the value of ``SHA-256(UTF8Encode("WebAuthn PRF") || 0x00 || ev.`second`)``.
		3. If the authenticator supports the CTAP2 `hmac-secret` extension [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)"):
		1. Send an `hmac-secret` extension to the [authenticator](#authenticator) using the values of salt1 and, if set, salt2 as the parameters of the same name in that process.
				2. Decrypt the extension result and set `results` to the PRF result(s), if any.
		4. If the authenticator does not support the CTAP2 `hmac-secret` extension [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)"), but does support some other implementation compatible with the abstract authenticator processing defined below:
		1. Use some unspecified mechanism to convey salt1 and, if defined, salt2 to the authenticator as PRF inputs, in that order.
				2. Use some unspecified mechanism to receive the PRF outputs from the authenticator as an `AuthenticationExtensionsPRFValues` value results. Set `` `results` `` to results.

Authenticator extension input / output

[This extension](#prf) is abstract over the authenticator implementation, using either the [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") `hmac-secret` extension or an unspecified interface for communication between the client and authenticator. It thus does not specify a CBOR interface for inputs and outputs.

Authenticator extension processing

[Authenticators](#authenticator) that support the [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") `hmac-secret` extension implement authenticator processing as defined in that extension.

[Authenticators](#authenticator) that do not support the [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") `hmac-secret` extension MAY instead implement the following abstract procedure:

1. Let PRF be the pseudo-random function associated with the current [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential), or initialize the association if uninitialized:
	Let PRF be a pseudo-random function whose outputs are exactly 32 bytes long, selected uniformly at random from a set of at least 2 <sup>256</sup> such functions. The choice of PRF MUST be independent of the state of [user verification](#user-verification). The selected PRF SHOULD NOT be used for other purposes than implementing this extension. Associate PRF with the current [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) for the lifetime of the credential.
2. Use some unspecified mechanism to receive PRF inputs salt1 and, optionally, salt2 from the [client](#client), in that order. If none are received, let salt1 and salt2 be undefined.
3. If salt1 is defined:
	1. Let results be an `AuthenticationExtensionsPRFValues` structure containing the evaluations of PRF at the given inputs:
		- Set `` results.`first` `` to `PRF(salt1)`.
				- If salt2 is defined, set `` results.`second` `` to `PRF(salt2)`.
		2. Use some unspecified mechanism to convey results to the [client](#client) as the PRF outputs.

Client extension output

```
dictionary AuthenticationExtensionsPRFOutputs {
    boolean enabled;
    AuthenticationExtensionsPRFValues results;
};
dictionary AuthenticationExtensionsPRFOutputsJSON {
    boolean enabled;
    AuthenticationExtensionsPRFValuesJSON results;
};

partial dictionary AuthenticationExtensionsClientOutputs {
    AuthenticationExtensionsPRFOutputs prf;
};
partial dictionary AuthenticationExtensionsClientOutputsJSON {
    AuthenticationExtensionsPRFOutputsJSON prf;
};
```

`enabled`, of type [boolean](https://webidl.spec.whatwg.org/#idl-boolean)

`true` if, and only if, the PRF is available for use with the created credential. This is only reported during [registration](#registration) and is not present in the case of [authentication](#authentication).

`results`, of type [AuthenticationExtensionsPRFValues](#dictdef-authenticationextensionsprfvalues)

The results of evaluating the PRF for the inputs given in `eval` or `evalByCredential`. Outputs may not be available during [registration](#registration); see comments in `eval`.

**For some use cases, for example if PRF outputs are used to derive encryption keys to use only on the client side, it may be necessary to omit this `results` output if the `PublicKeyCredential` is sent to a remote server, for example to perform the procedures in [§ 7 WebAuthn Relying Party Operations](#sctn-rp-operations). Note in particular that the `RegistrationResponseJSON` and `AuthenticationResponseJSON` returned by `` `PublicKeyCredential`.`toJSON()` `` will include this `results` output if present.**

#### 10.1.5. Large blob storage extension (largeBlob)

This [client](#client-extension) [registration extension](#registration-extension) and [authentication extension](#authentication-extension) allows a [Relying Party](#relying-party) to store opaque data associated with a credential. Since [authenticators](#authenticator) can only store small amounts of data, and most [Relying Parties](#relying-party) are online services that can store arbitrary amounts of state for a user, this is only useful in specific cases. For example, the [Relying Party](#relying-party) might wish to issue certificates rather than run a centralised authentication service.

Note: [Relying Parties](#relying-party) can assume that the opaque data will be compressed when being written to a space-limited device and so need not compress it themselves.

Since a certificate system needs to sign over the public key of the credential, and that public key is only available after creation, this extension does not add an ability to write blobs in the [registration](#registration-extension) context. However, [Relying Parties](#relying-party) SHOULD use the [registration extension](#registration-extension) when creating the credential if they wish to later use the [authentication extension](#authentication-extension).

Since certificates are sizable relative to the storage capabilities of typical authenticators, user agents SHOULD consider what indications and confirmations are suitable to best guide the user in allocating this limited resource and prevent abuse.

Note: In order to interoperate, user agents storing large blobs on authenticators using [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") are expected to use the provisions detailed in that specification for storing [large, per-credential blobs](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorLargeBlobs).

Note: [Roaming authenticators](#roaming-authenticators) that use [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") as their cross-platform transport protocol only support this [Large Blob](#largeblob) extension for [discoverable credentials](#discoverable-credential), and might return an error unless `` `authenticatorSelection`.`residentKey` `` is set to `preferred` or `required`. However, [authenticators](#authenticator) that do not utilize [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") do not necessarily restrict this extension to [discoverable credentials](#discoverable-credential).

Extension identifier

`largeBlob`

Operation applicability

[Registration](#registration-extension) and [authentication](#authentication-extension)

Client extension input

```
partial dictionary AuthenticationExtensionsClientInputs {
    AuthenticationExtensionsLargeBlobInputs largeBlob;
};
partial dictionary AuthenticationExtensionsClientInputsJSON {
    AuthenticationExtensionsLargeBlobInputsJSON largeBlob;
};

enum LargeBlobSupport {
  "required",
  "preferred",
};

dictionary AuthenticationExtensionsLargeBlobInputs {
    DOMString support;
    boolean read;
    BufferSource write;
};
dictionary AuthenticationExtensionsLargeBlobInputsJSON {
    DOMString support;
    boolean read;
    Base64URLString write;
};
```

`support`, of type [DOMString](https://webidl.spec.whatwg.org/#idl-DOMString)

A DOMString that takes one of the values of `LargeBlobSupport`. (See [§ 2.1.1 Enumerations as DOMString types](#sct-domstring-backwards-compatibility).) Only valid during [registration](#registration-extension).

`read`, of type [boolean](https://webidl.spec.whatwg.org/#idl-boolean)

A boolean that indicates that the [Relying Party](#relying-party) would like to fetch the previously-written blob associated with the asserted credential. Only valid during [authentication](#authentication-extension).

`write`, of type [BufferSource](https://webidl.spec.whatwg.org/#BufferSource)

An opaque byte string that the [Relying Party](#relying-party) wishes to store with the existing credential. Only valid during [authentication](#authentication-extension).

Client extension processing ([registration](#registration-extension))

1. If `read` or `write` is present:
	1. Return a `DOMException` whose name is “ `NotSupportedError` ”.
2. If `support` is present and has the value `required`:
	1. Set `supported` to `true`.
		Note: This is in anticipation of an authenticator capable of storing large blobs becoming available. It occurs during extension processing in [step 12](#CreateCred-process-extensions) of `[[Create]](origin, options, sameOriginWithAncestors)`. The `AuthenticationExtensionsLargeBlobOutputs` will be abandoned if no satisfactory authenticator becomes available.
		2. If a [candidate authenticator](#create-candidate-authenticator) becomes available ([step 22](#CreateCred-async-loop) of `[[Create]](origin, options, sameOriginWithAncestors)`) then, before evaluating any `options`, [continue](https://infra.spec.whatwg.org/#iteration-continue) (i.e. ignore the [candidate authenticator](#create-candidate-authenticator)) if the [candidate authenticator](#create-candidate-authenticator) is not capable of storing large blobs.
3. Otherwise (i.e. `support` is absent or has the value `preferred`):
	1. If an [authenticator is selected](#create-selected-authenticator) and the [selected authenticator](#create-selected-authenticator) supports large blobs, set `supported` to `true`, and `false` otherwise.

Client extension processing ([authentication](#authentication-extension))

1. If `support` is present:
	1. Return a `DOMException` whose name is “ `NotSupportedError` ”.
2. If both `read` and `write` are present:
	1. Return a `DOMException` whose name is “ `NotSupportedError` ”.
3. If `read` is present and has the value `true`:
	1. Initialize the [client extension output](#client-extension-output), `largeBlob`.
		2. If any authenticator indicates success (in `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)`), attempt to read any largeBlob data associated with the asserted credential.
		3. If successful, set `blob` to the result.
		Note: If the read is not successful, `largeBlob` will be present in `AuthenticationExtensionsClientOutputs` but the `blob` member will not be present.
4. If `write` is present:
	1. If `allowCredentials` does not contain exactly one element:
		1. Return a `DOMException` whose name is “ `NotSupportedError` ”.
		2. If the [assertion](#sctn-getAssertion) operation is successful, attempt to store the contents of `write` on the [authenticator](#authenticator), associated with the indicated credential.
		3. Set `written` to `true` if successful and `false` otherwise.

Client extension output

```
partial dictionary AuthenticationExtensionsClientOutputs {
    AuthenticationExtensionsLargeBlobOutputs largeBlob;
};
partial dictionary AuthenticationExtensionsClientOutputsJSON {
    AuthenticationExtensionsLargeBlobOutputsJSON largeBlob;
};

dictionary AuthenticationExtensionsLargeBlobOutputs {
    boolean supported;
    ArrayBuffer blob;
    boolean written;
};
dictionary AuthenticationExtensionsLargeBlobOutputsJSON {
    boolean supported;
    Base64URLString blob;
    boolean written;
};
```

`supported`, of type [boolean](https://webidl.spec.whatwg.org/#idl-boolean)

`true` if, and only if, the created credential supports storing large blobs. Only present in [registration](#registration-extension) outputs.

`blob`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer)

The opaque byte string that was associated with the credential identified by `rawId`. Only valid if `read` was `true`.

`written`, of type [boolean](https://webidl.spec.whatwg.org/#idl-boolean)

A boolean that indicates that the contents of `write` were successfully stored on the [authenticator](#authenticator), associated with the specified credential.

Authenticator extension processing

[This extension](#largeblob) directs the user-agent to cause the large blob to be stored on, or retrieved from, the authenticator. It thus does not specify any direct authenticator interaction for [Relying Parties](#relying-party).

### 10.2. Authenticator Extensions

This section defines extensions that are both [client extensions](#client-extension) and [authenticator extensions](#authenticator-extension).

#### 10.2.1. Signing extension (previewSign) version 4

This [authenticator](#authenticator-extension) [registration extension](#registration-extension) and [authentication extension](#authentication-extension) allows a [Relying Party](#relying-party) to sign arbitrary data using an asymmetric key pair associated with a [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) but different from the [credential key pair](#credential-key-pair). A [registration ceremony](#registration-ceremony) creates the signing key pair and emits the signing public key, and [authentication ceremonies](#authentication-ceremony) can use the signing private key to sign arbitrary data. The signing private key is held exclusively by the [authenticator](#authenticator).

The high-level usage flow is as follows:

1. To create a signing key pair, the [Relying Party](#relying-party) initiates a [registration ceremony](#registration-ceremony) and requests the extension. The [authenticator](#authenticator) returns a signing public key and a signing key handle for the key pair.
2. To sign some chosen data, the [Relying Party](#relying-party) initiates an [authentication ceremony](#authentication-ceremony) and requests the extension with one or more [signing key handles](#signing-key-handle) for key pairs eligible to perform the signature. The [authenticator](#authenticator) returns a signature over the given data. Unlike an [assertion signature](#assertion-signature), the given data is signed unaltered; the signed data does not include [authenticator data](#authenticator-data) or [client data](#client-data).
	This step can be repeated any number of times.

As a motivating example use case, a [Relying Party](#relying-party) could generate an asymmetric key pair and use the generated public key as verification material for a [verifiable credential](https://w3c.github.io/vc-data-model/#dfn-verifiable-credential). Proofs for such a verifiable credential could then be generated only by getting an assertion from the associated WebAuthn credential.

Each [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) can be associated with at most one signing key pair, and the [user presence](#concept-user-present) and [user verification](#user-verification) policy for the signing key pair is fixed at the time of creation. If additional signing key pairs are required, or signing key pairs with different [user presence](#concept-user-present) or [user verification](#user-verification) policies, the [Relying Party](#relying-party) MAY create a new [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) for each. In that case, the [Relying Party](#relying-party) SHOULD use a different [user handle](#user-handle) for each such [registration ceremony](#registration-ceremony), to avoid overwriting existing credentials, and SHOULD NOT specify the `excludeCredentials` parameter, to allow creating multiple credentials on the same [authenticator](#authenticator). Additional credentials created for this purpose SHOULD be stored and managed separately from ordinary authentication credentials, and SHOULD NOT be used for other purposes than signing data with the associated signing key pair.

[Attestation](#attestation) is supported for signing key pairs. This attestation signs over the same [RP ID](#rp-id), [authenticator data](#authenticator-data) [flags](#authdata-flags), [AAGUID](#aaguid) and [hash of the serialized client data](#collectedclientdata-hash-of-the-serialized-client-data) as the attestation for the associated [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential), but is not otherwise coupled to the associated [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential). The attestation also encodes the [user presence](#concept-user-present) and [user verification](#user-verification) policy of the signing key pair since unlike the associated credential, the signing key pair does not sign over an [authenticator data](#authenticator-data) structure.

Although this extension can be used with [discoverable credentials](#discoverable-credential), it does not support use with an empty `allowCredentials` because the intended use is for signing data that is meaningful on its own. This is unlike a random authentication challenge, which may be meaningless on its own and is used only to guarantee that an authentication signature was generated recently. In order to sign meaningful data, the [Relying Party](#relying-party) must first know what is to be signed, thus presumably must also first know which user is performing the signature, and thus also which signing keys are eligible. Thus, the signing use case is largely incompatible with the anonymous authentication challenge use case. Therefore the restriction to non-empty `allowCredentials` is unlikely to impose any additional restriction in practice, but does enable support for stateless [authenticator](#authenticator) implementations where neither the signing key pair nor the associated [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) need to consume storage space on the [authenticator](#authenticator).

Extension identifier

`previewSign`

Operation applicability

[Registration](#registration-extension) and [authentication](#authentication-extension)

Client extension input

```
partial dictionary AuthenticationExtensionsClientInputs {
    AuthenticationExtensionsSignInputs previewSign;
};

dictionary AuthenticationExtensionsSignInputs {
    AuthenticationExtensionsSignGenerateKeyInputs generateKey;
    record<USVString, AuthenticationExtensionsSignSignInputs> signByCredential;
};
```

`generateKey`, of type [AuthenticationExtensionsSignGenerateKeyInputs](#dictdef-authenticationextensionssigngeneratekeyinputs)

If present, the [authenticator](#authenticator) is requested to generate a new signing key pair and return the signing public key in the extension output.

This member MUST be present during a [registration ceremony](#registration-ceremony), and only during a [registration ceremony](#registration-ceremony).

`signByCredential`, of type record< [USVString](https://webidl.spec.whatwg.org/#idl-USVString), [AuthenticationExtensionsSignSignInputs](#dictdef-authenticationextensionssignsigninputs) >

A record mapping [base64url encoded](#base64url-encoding) [credential IDs](#credential-id) to signing inputs to use for each credential. If present, this MUST contain an [entry](https://infra.spec.whatwg.org/#map-entry) for each [credential ID](#credential-id) in `allowCredentials`, and no other entries.

This member MUST NOT be present during a [registration ceremony](#registration-ceremony), and MUST be present during an [authentication ceremony](#authentication-ceremony).

If the [authenticator](#authenticator) [contains](#contains) any credentials whose [credential IDs](#credential-id) appear as [keys](https://infra.spec.whatwg.org/#map-getting-the-keys), the [client](#client) selects one of them and sends the corresponding [value](https://infra.spec.whatwg.org/#map-value) to the authenticator as inputs to the signing algorithm. Otherwise, the [authentication ceremony](#authentication-ceremony) fails.

```
dictionary AuthenticationExtensionsSignGenerateKeyInputs {
    required sequence<COSEAlgorithmIdentifier> algorithms;
};
```

`algorithms`, of type sequence< [COSEAlgorithmIdentifier](#typedefdef-cosealgorithmidentifier) >

A list of acceptable signature algorithms, ordered from most preferred to least preferred. The [authenticator](#authenticator) will create a signing key pair for the most preferred algorithm possible. If none of the listed algorithms are supported, the [registration ceremony](#registration-ceremony) fails.

```
dictionary AuthenticationExtensionsSignSignInputs {
    required BufferSource keyHandle;
    required BufferSource tbs;
    COSESignArgs additionalArgs;
};

typedef BufferSource COSESignArgs;
```

`keyHandle`, of type [BufferSource](https://webidl.spec.whatwg.org/#BufferSource)

The key handle for the signing private key: auxiliary information that the [authenticator](#authenticator) might need in addition to the [credential ID](#credential-id) to look up or derive the signing private key.

A suitable value for this member MAY be retrieved from the `` `generatedKey`.`keyHandle` `` output.

`tbs`, of type [BufferSource](https://webidl.spec.whatwg.org/#BufferSource)

Data to be signed. The [authenticator](#authenticator) will sign this value using the signing private key.

Note: Depending on the signing algorithm, this may or may not need to be pre-hashed by the [Relying Party](#relying-party). See for example [\[I-D.cose-2p-algs\]](#biblio-i-dcose-2p-algs "COSE Algorithms for Two-Party Signing") for some definitions of signature algorithms that expect the [Relying Party](#relying-party) to pre-hash the data to be signed.

`additionalArgs`, of type [COSESignArgs](#typedefdef-cosesignargs)

Additional arguments to the signing algorithm, if needed. If present, this MUST contain a CBOR map encoding a COSE\_Sign\_Args object [\[I-D.cose-2p-algs\]](#biblio-i-dcose-2p-algs "COSE Algorithms for Two-Party Signing"). Refer to the definition of the COSE algorithm identifier for how to construct this value; if no instruction is given, omit this member.

`COSESignArgs` is a type alias for a `BufferSource` that MUST contain a CBOR map encoding a COSE\_Sign\_Args object [\[I-D.cose-2p-algs\]](#biblio-i-dcose-2p-algs "COSE Algorithms for Two-Party Signing").

Client extension processing ([registration](#registration-extension))

These extension processing steps use the variables pkOptions and credentialCreationData defined in [§ 5.1.3 Create a New Credential - PublicKeyCredential’s \[\[Create\]\](origin, options, sameOriginWithAncestors) Internal Method](#sctn-createCredential).

1. Let extSign denote `` pkOptions.`extensions`.`previewSign` ``.
2. If `` extSign.`generateKey` `` is not present, return a `DOMException` whose name is “ `NotSupportedError` ”.
3. If `` extSign.`signByCredential` `` is present, return a `DOMException` whose name is “ `NotSupportedError` ”.
4. Set the `previewSign` [authenticator extension input](#authenticator-extension-input) to a CBOR map with the entries:
	- `alg`: `` extSign.`generateKey`.`algorithms` `` encoded as a CBOR array of integers, in order.
		- `flags`: The CDDL value `0b101` if `` pkOptions.`authenticatorSelection`.`userVerification` `` is set to `required`, otherwise the CDDL value `0b001`.
5. After the [authenticatorMakeCredential](#authenticatormakecredential) operation is successful, let authData denote `credentialCreationData.attestationObjectResult["authData"]`. Let unsignedExtOutputs denote the [unsigned extension outputs](#unsigned-extension-outputs). Set the [client extension output](#client-extension-output) `` credentialCreationData.clientExtensionResults.`previewSign` `` to an `AuthenticationExtensionsSignOutputs` value with the members:
	- `generatedKey`: An `AuthenticationExtensionsSignGeneratedKey` value with the members:
		- `keyHandle`: An `ArrayBuffer` containing a copy of `innerAuthData.attestedCredentialData.credentialId`.
				- `publicKey`: An `ArrayBuffer` containing a copy of `innerAuthData.attestedCredentialData.credentialPublicKey`.
				- `algorithm`: A copy of `authData.extensions["previewSign"][alg]`.
				- `attestationObject`: An `ArrayBuffer` constructed as follows:
			1. Let origAttObj be `unsignedExtOutputs["previewSign"][att-obj]` parsed as a CBOR map.
						2. Let newAttObj be an empty CBOR map.
						3. Set `newAttObj["fmt"]` to `origAttObj[1]`.
						4. Set `newAttObj["authData"]` to `origAttObj[2]`.
						5. Set `newAttObj["attStmt"]` to `origAttObj[3]`.
						6. Set `attestationObject` to an `ArrayBuffer` containing newAttObj encoded in CBOR.

The CBOR map keys `alg`, `flags` and `att-obj` are aliases defined below in the CDDL for the [authenticator extension input](#authenticator-extension-input) and [authenticator extension output](#authenticator-extension-output).

Client extension processing ([authentication](#authentication-extension))

These extension processing steps use the variables pkOptions and assertionCreationData defined in [§ 5.1.4 Use an Existing Credential to Make an Assertion](#sctn-getAssertion).

1. Let extSign denote `` pkOptions.`extensions`.`previewSign` ``.
2. If `` extSign.`signByCredential` `` is not present, return a `DOMException` whose name is “ `NotSupportedError` ”.
3. If `` extSign.`generateKey` `` is present, return a `DOMException` whose name is “ `NotSupportedError` ”.
4. If `` pkOptions.`allowCredentials` `` [is empty](https://infra.spec.whatwg.org/#list-is-empty), return a `DOMException` whose name is “ `NotSupportedError` ”.
5. If the [size](https://infra.spec.whatwg.org/#map-size) of `` extSign.`signByCredential` `` does not equal the [size](https://infra.spec.whatwg.org/#list-size) of `` pkOptions.`allowCredentials` ``, return a `DOMException` whose name is “ `NotSupportedError` ”.
6. [For each](https://infra.spec.whatwg.org/#list-iterate) allowedCredential in `` pkOptions.`allowCredentials` ``:
	1. Let encodedCredentialId be the [base64url encoding](#base64url-encoding) of `` allowedCredential.`id` ``.
		2. Let signInputs be ``extSign.`signByCredential`[encodedCredentialId]``.
		3. If signInputs is undefined, return a `DOMException` whose name is “ `SyntaxError` ”.
7. Using some [client](#client) -specific procedure, determine which entries of `` pkOptions.`allowCredentials` `` are valid for the [authenticator](#authenticator). Let chosenCredentialId be an arbitrary choice of one of those entries. Let chosenCredentialIdB64 be the [base64url encoding](#base64url-encoding) of `` chosenCredentialId.`id` ``.
	Note: For example, for [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") authenticators this might be determined by invoking the CTAP2 `authenticatorGetAssertion` command with the `up` option set to `false`.
	If none are valid, abort these extension processing steps. Omit the `previewSign` [authenticator extension input](#authenticator-extension-input) and the `previewSign` [client extension output](#client-extension-output).
8. Let signInputs be ``extSign.`signByCredential`[chosenCredentialIdB64]``.
9. Set the `previewSign` authenticator extension input to a CBOR map with the entries:
	- `kh`: `` signInputs.`keyHandle` `` encoded as a CBOR byte string.
		- `tbs`: `` signInputs.`tbs` `` encoded as a CBOR byte string.
		- `args`: `` signInputs.`additionalArgs` `` encoded as a CBOR byte string.
10. After the [authenticatorGetAssertion](#authenticatorgetassertion) operation is successful, let authData denote `assertionCreationData.authenticatorDataResult`. Set the [client extension output](#client-extension-output) `` assertionCreationData.clientExtensionResults.`previewSign` `` to an `AuthenticationExtensionsSignOutputs` value with the members:
	- `generatedKey`: Omit this member.
		- `signature`: The [authenticator extension output](#authenticator-extension-output) `authData.extensions["previewSign"][sig]` parsed as an `ArrayBuffer`.

The CBOR map keys `kh`, `tbs`, `args` and `sig` are aliases defined below in the CDDL for the [authenticator extension input](#authenticator-extension-input) and [authenticator extension output](#authenticator-extension-output).

Client extension output

```
partial dictionary AuthenticationExtensionsClientOutputs {
    AuthenticationExtensionsSignOutputs previewSign;
};

dictionary AuthenticationExtensionsSignOutputs {
    AuthenticationExtensionsSignGeneratedKey generatedKey;
    ArrayBuffer                              signature;
};
```

`generatedKey`, of type [AuthenticationExtensionsSignGeneratedKey](#dictdef-authenticationextensionssigngeneratedkey)

The generated public key and [signing key handle](#signing-key-handle). Present if and only if the `generateKey` input was present.

`signature`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer)

The generated signature. Present if and only if the `signByCredential` input was present.

```
dictionary AuthenticationExtensionsSignGeneratedKey {
    required ArrayBuffer keyHandle;
    required ArrayBuffer publicKey;
    required COSEAlgorithmIdentifier algorithm;
    required ArrayBuffer attestationObject;
};
```

`keyHandle`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer)

The key handle for the signing private key: auxiliary information that the [authenticator](#authenticator) might need in addition to the [credential ID](#credential-id) to look up or derive the signing private key.

This value is REQUIRED but MAY be a zero-length byte array.

This member is intended for use by [Relying Parties](#relying-party) that do not request [attestation](#attestation). [Relying Parties](#relying-party) that request attestation SHOULD instead retrieve the key handle from the [credential ID](#credential-id) in the [attested credential data](#attested-credential-data) embedded in `attestationObject` after verifying that [attestation object](#attestation-object).

`publicKey`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer)

The generated signing public key in COSE\_Key format.

This member is intended for use by [Relying Parties](#relying-party) that do not request [attestation](#attestation). [Relying Parties](#relying-party) that request attestation SHOULD instead retrieve the generated public key from the [attested credential data](#attested-credential-data) embedded in `attestationObject` after verifying that [attestation object](#attestation-object).

`algorithm`, of type [COSEAlgorithmIdentifier](#typedefdef-cosealgorithmidentifier)

The algorithm identifier chosen from the `algorithms` input argument. This MAY be different from the `alg (3)` attribute of `publicKey`, for example if the chosen algorithm is a split algorithm [\[I-D.cose-2p-algs\]](#biblio-i-dcose-2p-algs "COSE Algorithms for Two-Party Signing") in which case `algorithm` SHOULD identify the split signing algorithm while the `alg (3)` attribute of `publicKey` SHOULD identify the corresponding verification procedure.

The RP MAY use this when constructing a COSE\_Sign\_Args structure [\[I-D.cose-2p-algs\]](#biblio-i-dcose-2p-algs "COSE Algorithms for Two-Party Signing") as the `additionalArgs` argument to subsequent signing operations.

This member is intended for use by [Relying Parties](#relying-party) that do not request [attestation](#attestation). [Relying Parties](#relying-party) that request attestation SHOULD instead retrieve the value from the [authenticator extension outputs](#authenticator-extension-output).

`attestationObject`, of type [ArrayBuffer](https://webidl.spec.whatwg.org/#idl-ArrayBuffer)

An [attestation object](#attestation-object) for the generated signing public key. This has the same structure as the top-level [attestation object](#attestation-object), except the `prewviewSign` [authenticator extension output](#authenticator-extension-output) contains a `flags` member indicating the [user verification](#user-verification) policy for the signing key instead of the `alg` and `sig` members.

Authenticator extension input

A CBOR map with the structure of the following CDDL:

```
; The symbolic names on the left are represented in CBOR by the integers on the right
kh = 2
alg = 3
flags = 4
tbs = 6
args = 7

$$extensionInput //= (
  previewSign: {
    ; Registration (key generation) input
    alg        => [ + COSEAlgorithmIdentifier ],
    ? flags    => &(unattended: 0b000,
                    require-up: 0b001,
                    require-uv: 0b101) .default 0b001,
    //
    ; Authentication (signing) input
    kh         => bstr,
    tbs        => bstr,
    ? args     => bstr .cbor COSE_Sign_Args,
  },
)
```

The CDDL type `COSE_Key_Ref` is defined in [\[I-D.cose-2p-algs\]](#biblio-i-dcose-2p-algs "COSE Algorithms for Two-Party Signing").

alg

A list of acceptable signature algorithms, ordered from most preferred to least preferred. MUST be present during [registration ceremonies](#registration-ceremony). MUST NOT be present during [authentication ceremonies](#authentication-ceremony).

The [authenticator](#authenticator) will create a signing key pair for the most preferred algorithm possible. If none of the listed algorithms are supported, the [registration ceremony](#registration-ceremony) fails.

flags

[Authenticator data](#authenticator-data) [flags](#authdata-flags) that MUST be set when generating a signature with this signing private key. MAY be present during [registration ceremonies](#registration-ceremony). MUST NOT be present during [authentication ceremonies](#authentication-ceremony).

- If `unattended` (`0b000`), signatures will not require [user presence](#concept-user-present) or [user verification](#user-verification).
- If `require-up` (`0b001`), signatures will require [user presence](#concept-user-present) but will not require [user verification](#user-verification).
- If `require-uv` (`0b101`), signatures will require [user presence](#concept-user-present) and [user verification](#user-verification).

If not present during a [registration ceremony](#registration-ceremony), the default is `require-up` (`0b001`).

This setting is recorded in the [attestation object](#attestation-object) for the signing key pair.

kh

The [signing key handle](#signing-key-handle) to use for generating the signature. MUST NOT be present during [registration ceremonies](#registration-ceremony). MUST be present during [authentication ceremonies](#authentication-ceremony).

A suitable value for this MAY be retrieved from `unsignedExtOutputs["previewSign"][att-obj]["authData"].attestedCredentialData.credentialId`, given the [unsigned extension outputs](#unsigned-extension-outputs) from the [registration ceremony](#registration-ceremony) as unsignedExtOutputs.

tbs

The data to be signed. MUST NOT be present during [registration ceremonies](#registration-ceremony). MUST be present during [authentication ceremonies](#authentication-ceremony).

args

Additional arguments to the signing algorithm, if needed by the signing algorithm. MUST NOT be present during [registration ceremonies](#registration-ceremony). MAY be present during [authentication ceremonies](#authentication-ceremony). If present, this MUST contain a CBOR map encoding a COSE\_Sign\_Args object [\[I-D.cose-2p-algs\]](#biblio-i-dcose-2p-algs "COSE Algorithms for Two-Party Signing").

Note: The `args` entry is defined as a byte string containing CBOR-encoded data instead of a direct CBOR map because the allows at most 4 levels of nested CBOR structures. If `args` were an unwrapped CBOR map, it could exceed this nesting limit if it in turn contains arrays or maps as values.

Authenticator extension processing ([registration](#registration-extension))

These processing steps use the hash, rpEntity, and extensions parameters and the attestationFormat variable in the [authenticatorMakeCredential](#authenticatormakecredential) operation. Let extSign denote `extensions["previewSign"]`. Let authData denote the [authenticator data](#authenticator-data) that will be returned from the [authenticatorMakeCredential](#authenticatormakecredential) operation.

1. Let auxIkm denote some, possibly empty, random entropy and/or auxiliary data of the [authenticator’s](#authenticator) choice to be used to generate a signing key pair.
2. Let chosenAlg be null.
3. [For each](https://infra.spec.whatwg.org/#list-iterate) candidateAlg in `extSign[alg]`:
	1. If the [authenticator](#authenticator) supports candidateAlg for signing operations, let chosenAlg be candidateAlg and [break](https://infra.spec.whatwg.org/#iteration-break).
4. If chosenAlg is null, return an error code equivalent to " `NotSupportedError` " and terminate the operation. Implementations in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return the error code `CTAP2_ERR_UNSUPPORTED_ALGORITHM`.
5. Let signFlags be the value of `extSign[flags]`.
6. If signFlags is not one of the values `unattended` (`0b000`), `require-up` (`0b001`) or `require-uv` (`0b101`), return an error code equivalent to " `SyntaxError` " and terminate these processing steps. Implementations in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return the error code `CTAP2_ERR_INVALID_OPTION`.
7. Use signFlags, auxIkm and a per-credential authenticator secret as the seeds to deterministically generate a new key pair for the algorithm chosenAlg. Let p be the generated private key and P be the corresponding public key.
8. Let kh be byte string containing an authenticator-specific encoding of chosenAlg, signFlags and auxIkm, which the authenticator can later use to re-generate the same key pair p, P. The encoding SHOULD include integrity protection to ensure that a given kh is valid for a particular authenticator. kh MAY be empty if the authenticator can store equivalent information internally.
	An example implementation of this encoding is given in [§ 10.2.1.1 Example key handle encoding](#sctn-sign-extension-example-key-handle-encoding).
9. Set `authData.extensions["previewSign"]` to a new CBOR map with the entries:
	- `alg`: chosenAlg.
10. Set the [unsigned extension output](#unsigned-extension-outputs) `"previewSign"` to a new CBOR map with the entries:
	- `att-obj`: a CBOR byte array encoding an [attestation object](#attestation-object) generated as described in [§ 6.5.4 Generating an Attestation Object](#sctn-generating-an-attestation-object) using the inputs:
		- attestationFormat: attestationFormat.
				- authData: an [authenticator data](#authenticator-data) structure with the contents:
			- [rpIdHash](#authdata-rpidhash): `authData.rpIdHash`.
						- [flags](#authdata-flags): `authData.flags`.
						- [signCount](#authdata-signcount): 0.
						- [attestedCredentialData](#authdata-attestedcredentialdata): An [attested credential data](#attested-credential-data) structure with the contents:
				- [aaguid](#authdata-attestedcredentialdata-aaguid): `authData.attestedCredentialData.aaguid`.
								- [credentialIdLength](#authdata-attestedcredentialdata-credentialidlength): The length of kh.
								- [credentialId](#authdata-attestedcredentialdata-credentialid): kh encoded in CBOR.
								- [credentialPublicKey](#authdata-attestedcredentialdata-credentialpublickey): P encoded as a COSE\_Key map.
						- [extensions](#authdata-extensions): A CBOR map with the entries:
				- `"previewSign"`: A CBOR map with the entries:
					- `flags`: signFlags encoded as a CBOR unsigned integer.
				Note: The `"previewSign"` key here is a CDDL text string literal, but `flags` is an alias of an integer value.
				- hash: hash.

The CBOR map keys `alg`, `flags`, `kh`, `args`, and `att-obj` are aliases defined above and below in the CDDL for the [authenticator extension input](#authenticator-extension-input) and [authenticator extension output](#authenticator-extension-output).

Authenticator extension processing ([authentication](#authentication-extension))

Using the extensions argument to the [authenticatorGetAssertion](#authenticatorgetassertion) operation, let extSign denote `extensions["previewSign"]`. Let authData denote the [authenticator data](#authenticator-data) that will be returned from the [authenticatorGetAssertion](#authenticatorgetassertion) operation.

1. If allowCredentialDescriptorList is empty, return an error code equivalent to " `NotAllowedError` " and terminate these processing steps. Implementations in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return the error code `CTAP2_ERR_INVALID_OPTION`.
2. If `extSign[kh]` is not present or `extSign[tbs]` is not present, return an error code equivalent to " `UnknownError` " and terminate these processing steps. Implementations in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return the error code `CTAP2_ERR_INVALID_OPTION`.
3. Let kh be `extSign[kh]`.
4. Decode the authenticator-specific encoding of `extSign[kh]` to extract the encoded chosenAlg, signFlags and auxIkm. This procedure SHOULD verify integrity to ensure that `extSign[kh]` was generated by this authenticator.
	An example implementation of this decoding is given in [§ 10.2.1.1 Example key handle encoding](#sctn-sign-extension-example-key-handle-encoding).
5. If `extSign[args]` is present, let args be `extSign[args]` decoded as a COSE\_Sign\_Args strucure [\[I-D.cose-2p-algs\]](#biblio-i-dcose-2p-algs "COSE Algorithms for Two-Party Signing"). Otherwise let args be null.
6. If args is not null and `args[alg]` is not present or does not equal chosenAlg, return an error code equivalent to " `NotSupportedError` " and terminate the operation. Implementations in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return the error code `CTAP2_ERR_INVALID_CREDENTIAL`.
7. If args is null and the signing algorithm identified by chosenAlg requires additional arguments, return an error code equivalent to " `DataError` " and terminate these processing steps. Implementations in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return the error code `CTAP2_ERR_MISSING_PARAMETER`.
8. If the [UP](#authdata-flags-up) bit is set in signFlags but not in `authData.flags`, return an error code equivalent to " `ConstraintError` " and terminate the operation. Implementations in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return the error code `CTAP2_ERR_UP_REQUIRED`.
9. If the [UV](#authdata-flags-uv) bit is set in signFlags but not in `authData.flags`, return an error code equivalent to " `ConstraintError` " and terminate the operation. Implementations in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return the error code `CTAP2_ERR_PUAT_REQUIRED`.
10. Use signFlags, auxIkm, and a per-credential authenticator secret as the seeds to deterministically re-generate the key pair with private key p and public key P for the algorithm chosenAlg.
11. Set `authData.extensions["previewSign"]` to a new CBOR map with the entries:
	- `sig`: The result of signing `extSign[tbs]`, with additional signing arguments args if present and used by the signing algorithm, using the private key referenced by kh.

The CBOR map keys `kh`, `tbs`, `args`, `alg` and `sig` are aliases defined above and below in the CDDL for the [authenticator extension input](#authenticator-extension-input) and [authenticator extension output](#authenticator-extension-output).

Authenticator extension output

A CBOR map with the structure of the following CDDL:

```
; The symbolic names on the left are represented in CBOR by the integers on the right
alg = 3
flags = 4
sig = 6

$$extensionOutput //= (
  previewSign: {
    ; Registration (key generation) outputs
    alg     => COSEAlgorithmIdentifier   ; Algorithm chosen from alg input
    //
    ; Authentication (signing) outputs
    ; This choice is redundant given the one above, but is there to emphasize
    ; that \`sig\` is required in authentication ceremony outputs.
    sig     => bstr,               ; Signature over tbs input
    //
    ; Attestation fields
    flags   => &(unattended: 0b000,
                 require-up: 0b001,
                 require-uv: 0b101)
  },
)
```

Note: The `att-obj` entry is defined as a byte string containing CBOR-encoded data instead of a direct CBOR map because the allows at most 4 levels of nested CBOR structures.

alg

The `COSEAlgorithmIdentifier` for the signature algorithm chosen from the `alg` input. MUST be present in [registration ceremonies](#registration-ceremony). MUST NOT be present in [authentication ceremonies](#authentication-ceremony).

sig

A signature over the extension input `tbs`, if present, by the signing private key. MAY be present in [registration ceremonies](#registration-ceremony). MUST be present in [authentication ceremonies](#authentication-ceremony).

flags

A copy of the `flags` input. Present only in the [attestation object](#attestation-object) embedded within the `att-obj` output during [registration ceremonies](#registration-ceremony). This represents whether signing operations with this signing private key require [user presence](#concept-user-present) and [user verification](#user-verification):

- If `unattended` (`0b000`), signatures do not require [user presence](#concept-user-present) or [user verification](#user-verification).
- If `require-up` (`0b001`), signatures require [user verification](#user-verification) but do not require [user presence](#concept-user-present).
- If `require-uv` (`0b101`), signatures require [user presence](#concept-user-present) and [user verification](#user-verification).

The [unsigned extension output](#unsigned-extension-outputs) is a CBOR map with the structure of the following CDDL:

```
; The symbolic names on the left are represented in CBOR by the integers on the right
att-obj = 7

$$unsignedExtensionOutput //= (
  previewSign: {
    ; Registration (key generation) outputs
    att-obj => bstr .cbor attObj,  ; Attestation object for signing key pair
  },
)
```

att-obj

An [attestation object](#attestation-object) for the signing key pair.

Note that [unsigned extension output](#unsigned-extension-outputs) is only present in [registration ceremonies](#registration-ceremony).

##### 10.2.1.1. Example key handle encoding

This section defines one possible implementation of the encoding and decoding of the [signing key handle](#signing-key-handle) kh in the [authenticator extension processing](#authenticator-extension-processing) steps defined above. [Authenticator](#authenticator) implementations MAY use these encoding and decoding procedures, or MAY use different encodings with the same inputs and outputs.

To encode chosenAlg, signFlags and auxIkm, producing the output kh, perform the following steps:

1. Let macKey be a per-credential authenticator secret.
2. Let khParams be a CBOR array with the items:
	1. chosenAlg encoded as a CBOR integer.
		2. signFlags encoded as a CBOR unsigned integer.
		3. auxIkm encoded as a CBOR byte string.
3. Let khMac be the output of HMAC-SHA-256 [\[RFC2104\]](#biblio-rfc2104 "HMAC: Keyed-Hashing for Message Authentication") with the inputs:
	- Secret key `K`: macKey
		- Input `text`: `khParams || UTF8Encode("previewSign") || authData.rpIdHash`.
4. Let kh be `khMac || khParams`.

To decode kh, producing the output chosenAlg, signFlags and auxIkm, perform the following steps:

1. Let macKey be a per-credential authenticator secret.
2. Let mac be the first 32 bytes of kh and let khParams be the remaining bytes of kh after removing the first 32 bytes.
3. Verify that mac equals the output of HMAC-SHA-256 [\[RFC2104\]](#biblio-rfc2104 "HMAC: Keyed-Hashing for Message Authentication") with the inputs:
	- Secret key `K`: macKey
		- Input `text`: `khParams || UTF8Encode("previewSign") || authData.rpIdHash`.
	If not, this kh was generated by a different authenticator. Return an error code equivalent to " `NotAllowedError` " and terminate the extension processing steps. Implementations in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return the error code `CTAP2_ERR_INVALID_CREDENTIAL`.
4. Parse khParams as a CBOR array.
5. Let chosenAlg be `khParams[0]`.
6. Let signFlags be `khParams[1]`.
7. Let auxIkm be `khParams[2]`.

##### 10.2.1.2. Revision History

*This section is not normative.*

- Version 4
	- Published: 2025-08-26
		- Changed extension identifier to `previewSign` in preparation for broader prototype availability.
		- Changed authenticator error from `CTAP2_ERR_INVALID_CREDENTIAL` to `CTAP2_ERR_UNSUPPORTED_ALGORITHM` when no supported algorithm is found during registration.
		- Reworked COSE\_Key\_Ref to COSE\_Sign\_Args:
		- Input `sign: AuthenticationExtensionsSignSignInputs` replaced by `signByCredential: record<USVString, AuthenticationExtensionsSignSignInputs>`
				- The role previously held by `sign.keyHandleByCredential` is now taken by `signByCredential` for credential indexing, and by `signByCredential.additionalArgs` for carrying a COSE\_Sign\_Args instead of a COSE\_Key\_Ref.
				- Signing key handles are moved from the `kid` attribute of the COSE\_Key\_Ref to the `generatedKey.keyHandle` client output and the credential ID embedded in the unsigned authenticator output, and to the `signByCredential.keyHandle` client input and `kh` authenticator input.
				- Authenticator input `key-ref` replaced by `kh`.
				- Authenticator input `args` added.
				- Authenticator output `sig` deleted from registration outputs.
				- Client output `generatedKey.keyHandle` added.
				- Emphasized that client output `generatedKey.algorithm` value may differ from `alg` attribute of signing public key.
				- Added authenticator processing steps for processing the new `args` input.
				- Deleted section "Constructing a key handle from a COSE\_Key".
				- Variables renamed from "kid" to "kh" in section "Example key handle encoding".
				- Specified CTAP2 error code when key handle fails integrity check in example key handle encoding.
		- Deleted `generateKey.tbs` input. This won’t work in general with a mix of signing algorithms with different preconditions, and we can’t feasibly send an array of `{ alg: int, tbs: bstr, ?args: bstr .cbor COSE_Sign_Args }` to the authenticator.
- Version 3
	- Published: 2025-05-19
		- Client: Fixed CBOR map key in reference to authenticator data embedded in unsigned extension output.
		- Editorial and formatting fixes.
- Version 2
	- Published: 2025-04-07
		- Changed error code when `allowList` is empty
		- Moved `att-obj` from authenticator data to unsigned extension outputs and client extension outputs
		- Changed `key-refs: [+ bstr]` authenticator input to single `key-ref: bstr`
		- Reference [\[I-D.cose-2p-algs\]](#biblio-i-dcose-2p-algs "COSE Algorithms for Two-Party Signing") instead of ARKG for definition of COSE\_Key\_Ref
		- Deleted `generatedKey.keyHandle` client extension output
		- Added `alg` authenticator output and `generatedKey.algorithm` client output
		- Renamed `phData` input to `tbs`
		- Removed assumption of `tbs` being pre-hashed by the RP; this may instead be signaled using distinct COSEAlgorithmIdentifier values in the `generateKey.algorithms` input.
		- Changed CBOR alias `tbs = 0` (previously `phData = 0`) to `tbs = 6`
- Version 1
	- Published: 2024-09-11
		- Initial port from [https://github.com/w3c/webauthn/pull/2078](https://github.com/w3c/webauthn/pull/2078)

## 11\. User Agent Automation

For the purposes of user agent automation and [web application](#web-application) testing, this document defines a number of [\[WebDriver\]](#biblio-webdriver "WebDriver") [extension commands](https://w3c.github.io/webdriver/#dfn-extension-commands).

### 11.1. WebAuthn WebDriver Extension Capability

In order to advertise the availability of the [extension commands](https://w3c.github.io/webdriver/#dfn-extension-commands) defined below, a new [extension capability](https://w3c.github.io/webdriver/#dfn-extension-capability) is defined.

| Capability | Key | Value Type | Description |
| --- | --- | --- | --- |
| Virtual Authenticators Support | `"webauthn:virtualAuthenticators"` | boolean | Indicates whether the [endpoint node](https://w3c.github.io/webdriver/#dfn-endpoint-node) supports all [Virtual Authenticators](#virtual-authenticators) commands. |

When [validating capabilities](https://w3c.github.io/webdriver/#dfn-validate-capabilities), the extension-specific substeps to validate `"webauthn:virtualAuthenticators"` with `value` are the following:

1. If `value` is not a [boolean](https://infra.spec.whatwg.org/#boolean) return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
2. Otherwise, let `deserialized` be set to `value`.

When [matching capabilities](https://w3c.github.io/webdriver/#dfn-matching-capabilities), the extension-specific steps to match `"webauthn:virtualAuthenticators"` with `value` are the following:

1. If `value` is `true` and the [endpoint node](https://w3c.github.io/webdriver/#dfn-endpoint-node) does not support any of the [Virtual Authenticators](#virtual-authenticators) commands, the match is unsuccessful.
2. Otherwise, the match is successful.

#### 11.1.1. Authenticator Extension Capabilities

Additionally, [extension capabilities](https://w3c.github.io/webdriver/#dfn-extension-capability) are defined for every [authenticator extension](#authenticator-extension) (i.e. those defining [authenticator extension processing](#authenticator-extension-processing)) defined in this specification:

| Capability | Key | Value Type | Description |
| --- | --- | --- | --- |
| Pseudo-Random Function Extension Support | `"webauthn:extension:prf"` | boolean | Indicates whether the [endpoint node](https://w3c.github.io/webdriver/#dfn-endpoint-node) WebAuthn WebDriver implementation supports the [prf](#prf) extension. |
| Large Blob Storage Extension Support | `"webauthn:extension:largeBlob"` | boolean | Indicates whether the [endpoint node](https://w3c.github.io/webdriver/#dfn-endpoint-node) WebAuthn WebDriver implementation supports the [largeBlob](#largeblob) extension. |
| credBlob Extension Support | `"webauthn:extension:credBlob"` | boolean | Indicates whether the [endpoint node](https://w3c.github.io/webdriver/#dfn-endpoint-node) WebAuthn WebDriver implementation supports the `credBlob` extension defined in [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)"). |

When [validating capabilities](https://w3c.github.io/webdriver/#dfn-validate-capabilities), the extension-specific substeps to validate an [authenticator extension capability](#authenticator-extension-capabilities) `key` with `value` are the following:

1. If `value` is not a [boolean](https://infra.spec.whatwg.org/#boolean) return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
2. Otherwise, let `deserialized` be set to `value`.

When [matching capabilities](https://w3c.github.io/webdriver/#dfn-matching-capabilities), the extension-specific steps to match an [authenticator extension capability](#authenticator-extension-capabilities) `key` with `value` are the following:

1. If `value` is `true` and the [endpoint node](https://w3c.github.io/webdriver/#dfn-endpoint-node) WebAuthn WebDriver implementation does not support the [authenticator extension](#authenticator-extension) identified by the `key`, the match is unsuccessful.
2. Otherwise, the match is successful.

User-Agents implementing defined [authenticator extensions](#authenticator-extension) SHOULD implement the corresponding [authenticator extension capability](#authenticator-extension-capabilities).

### 11.2. Virtual Authenticators

These WebDriver [extension commands](https://w3c.github.io/webdriver/#dfn-extension-commands) create and interact with [Virtual Authenticators](#virtual-authenticators): software implementations of the [Authenticator Model](#authenticator-model). [Virtual Authenticators](#virtual-authenticators) are stored in a Virtual Authenticator Database. Each stored [virtual authenticator](#virtual-authenticators) has the following properties:

authenticatorId

An non-null string made using up to 48 characters from the `unreserved` production defined in Appendix A of [\[RFC3986\]](#biblio-rfc3986 "Uniform Resource Identifier (URI): Generic Syntax") that uniquely identifies the [Virtual Authenticator](#virtual-authenticators).

protocol

The protocol the [Virtual Authenticator](#virtual-authenticators) speaks: one of `"ctap1/u2f"`, `"ctap2"` or `"ctap2_1"` [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)").

transport

The `AuthenticatorTransport` simulated. If the transport is set to `internal`, the authenticator simulates [platform attachment](#platform-attachment). Otherwise, it simulates [cross-platform attachment](#cross-platform-attachment).

hasResidentKey

If set to `true` the authenticator will support [client-side discoverable credentials](#client-side-discoverable-credential).

hasUserVerification

If set to `true`, the authenticator supports [user verification](#user-verification).

isUserConsenting

Determines the result of all [authorization gestures](#authorization-gesture), and by extension, any [test of user presence](#test-of-user-presence) performed on the [Virtual Authenticator](#virtual-authenticators). If set to `true`, a will always be granted. If set to `false`, it will not be granted.

isUserVerified

Determines the result of [User Verification](#user-verification) performed on the [Virtual Authenticator](#virtual-authenticators). If set to `true`, [User Verification](#user-verification) will always succeed. If set to `false`, it will fail.

Note: This property has no effect if hasUserVerification is set to `false`.

extensions

A string array containing the [extension identifiers](#extension-identifier) supported by the [Virtual Authenticator](#virtual-authenticators).

A [Virtual authenticator](#virtual-authenticators) MUST support all [authenticator extensions](#authenticator-extension) present in its extensions array. It MUST NOT support any [authenticator extension](#authenticator-extension) not present in its extensions array.

defaultBackupEligibility

Determines the default state of the [backup eligibility](#backup-eligibility) [credential property](#credential-properties) for any newly created [Public Key Credential Source](#public-key-credential-source). This value MUST be reflected by the [BE](#authdata-flags-be) [authenticator data](#authenticator-data) [flag](#authdata-flags) when performing an [authenticatorMakeCredential](#authenticatormakecredential) operation with this [virtual authenticator](#virtual-authenticators).

defaultBackupState

Determines the default state of the [backup state](#backup-state) [credential property](#credential-properties) for any newly created [Public Key Credential Source](#public-key-credential-source). This value MUST be reflected by the [BS](#authdata-flags-bs) [authenticator data](#authenticator-data) [flag](#authdata-flags) when performing an [authenticatorMakeCredential](#authenticatormakecredential) operation with this [virtual authenticator](#virtual-authenticators).

### 11.3. Add Virtual Authenticator

The [Add Virtual Authenticator](#add-virtual-authenticator) WebDriver [extension command](https://w3c.github.io/webdriver/#dfn-extension-commands) creates a software [Virtual Authenticator](#virtual-authenticators). It is defined as follows:

| HTTP Method | URI Template |
| --- | --- |
| POST | `/session/{session id}/webauthn/authenticator` |

The Authenticator Configuration is a JSON [Object](https://w3c.github.io/webdriver/#dfn-object) passed to the [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) as parameters. It contains the following key and value pairs:

| Key | Value Type | Valid Values | Default |
| --- | --- | --- | --- |
| protocol | string | `"ctap1/u2f"`, `"ctap2"`, `"ctap2_1"` | None |
| transport | string | `AuthenticatorTransport` values | None |
| hasResidentKey | boolean | `true`, `false` | `false` |
| hasUserVerification | boolean | `true`, `false` | `false` |
| isUserConsenting | boolean | `true`, `false` | `true` |
| isUserVerified | boolean | `true`, `false` | `false` |
| extensions | string array | An array containing [extension identifiers](#extension-identifier) | Empty array |
| defaultBackupEligibility | boolean | `true`, `false` | `false` |
| defaultBackupState | boolean | `true`, `false` | `false` |

The [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) are:

1. If parameters is not a JSON [Object](https://w3c.github.io/webdriver/#dfn-object), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
	Note: parameters is an [Authenticator Configuration](#authenticator-configuration) object.
2. Let authenticator be a new [Virtual Authenticator](#virtual-authenticators).
3. For each enumerable [own property](https://tc39.github.io/ecma262/#sec-own-property) in parameters:
	1. Let key be the name of the property.
		2. Let value be the result of [getting a property](https://w3c.github.io/webdriver/#dfn-getting-properties) named key from parameters.
		3. If there is no matching `key` for key in [Authenticator Configuration](#authenticator-configuration), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
		4. If value is not one of the `valid values` for that key, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
		5. [Set a property](https://w3c.github.io/webdriver/#dfn-set-a-property) key to value on authenticator.
4. For each property in [Authenticator Configuration](#authenticator-configuration) with a default defined:
	1. If `key` is not a defined property of authenticator, [set a property](https://w3c.github.io/webdriver/#dfn-set-a-property) `key` to `default` on authenticator.
5. For each property in [Authenticator Configuration](#authenticator-configuration):
	1. If `key` is not a defined property of authenticator, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
6. For each extension in authenticator.extensions:
	1. If extension is not an [extension identifier](#extension-identifier) supported by the [endpoint node](https://w3c.github.io/webdriver/#dfn-endpoint-node) WebAuthn WebDriver implementation, return a with [unsupported operation](https://w3c.github.io/webdriver/#dfn-unsupported-operation).
7. Generate a valid unique [authenticatorId](#authenticatorid).
8. [Set a property](https://w3c.github.io/webdriver/#dfn-set-a-property) `authenticatorId` to authenticatorId on authenticator.
9. Store authenticator in the [Virtual Authenticator Database](#virtual-authenticator-database).
10. Return [success](https://w3c.github.io/webdriver/#dfn-success) with data authenticatorId.

### 11.4. Remove Virtual Authenticator

The [Remove Virtual Authenticator](#remove-virtual-authenticator) WebDriver [extension command](https://w3c.github.io/webdriver/#dfn-extension-commands) removes a previously created [Virtual Authenticator](#virtual-authenticators). It is defined as follows:

| HTTP Method | URI Template |
| --- | --- |
| DELETE | `/session/{session id}/webauthn/authenticator/{authenticatorId}` |

The [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) are:

1. If authenticatorId does not match any [Virtual Authenticator](#virtual-authenticators) stored in the [Virtual Authenticator Database](#virtual-authenticator-database), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
2. Remove the [Virtual Authenticator](#virtual-authenticators) identified by authenticatorId from the [Virtual Authenticator Database](#virtual-authenticator-database)
3. Return [success](https://w3c.github.io/webdriver/#dfn-success).

### 11.5. Add Credential

The [Add Credential](#add-credential) WebDriver [extension command](https://w3c.github.io/webdriver/#dfn-extension-commands) injects a [Public Key Credential Source](#public-key-credential-source) into an existing [Virtual Authenticator](#virtual-authenticators). It is defined as follows:

| HTTP Method | URI Template |
| --- | --- |
| POST | `/session/{session id}/webauthn/authenticator/{authenticatorId}/credential` |

The Credential Parameters is a JSON [Object](https://w3c.github.io/webdriver/#dfn-object) passed to the [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) as parameters. It contains the following key and value pairs:

| Key | Description | Value Type |
| --- | --- | --- |
| credentialId | The [Credential ID](#public-key-credential-source-id) encoded using [Base64url Encoding](#base64url-encoding). | string |
| isResidentCredential | If set to `true`, a [client-side discoverable credential](#client-side-discoverable-credential) is created. If set to `false`, a [server-side credential](#server-side-credential) is created instead. | boolean |
| rpId | The [Relying Party ID](#public-key-credential-source-rpid) the credential is scoped to. | string |
| privateKey | An asymmetric key package containing a single [private key](#public-key-credential-source-privatekey) per [\[RFC5958\]](#biblio-rfc5958 "Asymmetric Key Packages"), encoded using [Base64url Encoding](#base64url-encoding). | string |
| userHandle | The [userHandle](#public-key-credential-source-userhandle) associated to the credential encoded using [Base64url Encoding](#base64url-encoding). This property may not be defined. | string |
| signCount | The initial value for a [signature counter](#signature-counter) associated to the [public key credential source](#public-key-credential-source). | number |
| largeBlob | The [large, per-credential blob](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorLargeBlobs) associated to the [public key credential source](#public-key-credential-source), encoded using [Base64url Encoding](#base64url-encoding). This property may not be defined. | string |
| backupEligibility | The simulated [backup eligibility](#backup-eligibility) for the [public key credential source](#public-key-credential-source). If unset, the value will default to the [virtual authenticator](#virtual-authenticators) ’s defaultBackupEligibility property. The simulated [backup eligibility](#backup-eligibility) MUST be reflected by the [BE](#authdata-flags-be) [authenticator data](#authenticator-data) [flag](#authdata-flags) when performing an [authenticatorGetAssertion](#authenticatorgetassertion) operation with this [public key credential source](#public-key-credential-source). | boolean |
| backupState | The simulated [backup state](#backup-state) for the [public key credential source](#public-key-credential-source). If unset, the value will default to the [virtual authenticator](#virtual-authenticators) ’s defaultBackupState property. The simulated [backup state](#backup-state) MUST be reflected by the [BS](#authdata-flags-bs) [authenticator data](#authenticator-data) [flag](#authdata-flags) when performing an [authenticatorGetAssertion](#authenticatorgetassertion) operation with this [public key credential source](#public-key-credential-source). | boolean |
| userName | The `user` ’s `name` associated to the credential. If unset, the value will default to the empty string. | string |
| userDisplayName | The `user` ’s `displayName` associated to the credential. If unset, the value will default to the empty string. | string |

The [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) are:

1. If parameters is not a JSON [Object](https://w3c.github.io/webdriver/#dfn-object), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
	Note: parameters is a [Credential Parameters](#credential-parameters) object.
2. Let credentialId be the result of decoding [Base64url Encoding](#base64url-encoding) on the parameters ’ credentialId property.
3. If credentialId is failure, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
4. Let isResidentCredential be the parameters ’ isResidentCredential property.
5. If isResidentCredential is not defined, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
6. Let rpId be the parameters ’ rpId property.
7. If rpId is not a valid [RP ID](#rp-id), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
8. Let privateKey be the result of decoding [Base64url Encoding](#base64url-encoding) on the parameters ’ privateKey property.
9. If privateKey is failure, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
10. If privateKey is not a validly-encoded asymmetric key package containing a single ECDSA private key on the P-256 curve per [\[RFC5958\]](#biblio-rfc5958 "Asymmetric Key Packages"), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
11. If the parameters ’ userHandle property is defined:
	1. Let userHandle be the result of decoding [Base64url Encoding](#base64url-encoding) on the parameters ’ userHandle property.
		2. If userHandle is failure, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
12. Otherwise:
	1. If isResidentCredential is `true`, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
		2. Let userHandle be `null`.
13. If authenticatorId does not match any [Virtual Authenticator](#virtual-authenticators) stored in the [Virtual Authenticator Database](#virtual-authenticator-database), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
14. Let authenticator be the [Virtual Authenticator](#virtual-authenticators) matched by authenticatorId.
15. If isResidentCredential is `true` and the authenticator ’s hasResidentKey property is `false`, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
16. If the authenticator supports the [largeBlob](#largeblob) extension and the parameters ’ largeBlob feature is defined:
	1. Let largeBlob be the result of decoding [Base64url Encoding](#base64url-encoding) on the parameters ’ largeBlob property.
		2. If largeBlob is failure, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
17. Otherwise:
	1. Let largeBlob be `null`.
18. Let backupEligibility be the parameters ’ backupEligibility property.
19. If backupEligibility is not defined, set backupEligibility to the value of the authenticator ’s defaultBackupEligibility.
20. Let backupState be the parameters ’ backupState property.
21. If backupState is not defined, set backupState to the value of the authenticator ’s defaultBackupState.
22. Let userName be the parameters ’ userName property.
23. If userName is not defined, set userName to the empty string.
24. Let userDisplayName be the parameters ’ userDisplayName property.
25. If userDisplayName is not defined, set userDisplayName to the empty string.
26. Let credential be a new [Client-side discoverable Public Key Credential Source](#client-side-discoverable-public-key-credential-source) if isResidentCredential is `true` or a [Server-side Public Key Credential Source](#server-side-public-key-credential-source) otherwise whose items are:
	[type](#public-key-credential-source-type)
	`public-key`
	[id](#public-key-credential-source-id)
	credentialId
	[privateKey](#public-key-credential-source-privatekey)
	privateKey
	[rpId](#public-key-credential-source-rpid)
	rpId
	[userHandle](#public-key-credential-source-userhandle)
	userHandle
	[otherUI](#public-key-credential-source-otherui)
	Construct from userName and userDisplayName.
27. Set the credential ’s [backup eligibility](#backup-eligibility) [credential property](#credential-properties) to backupEligibility.
28. Set the credential ’s [backup state](#backup-state) [credential property](#credential-properties) to backupState.
29. Associate a [signature counter](#signature-counter) counter to the credential with a starting value equal to the parameters ’ signCount or `0` if signCount is `null`.
30. If largeBlob is not `null`, set the [large, per-credential blob](https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-errata-20220621.html#authenticatorLargeBlobs) associated to the credential to largeBlob.
31. Store the credential and counter in the database of the authenticator.
32. Return [success](https://w3c.github.io/webdriver/#dfn-success).

### 11.6. Get Credentials

The [Get Credentials](#get-credentials) WebDriver [extension command](https://w3c.github.io/webdriver/#dfn-extension-commands) returns one [Credential Parameters](#credential-parameters) object for every [Public Key Credential Source](#public-key-credential-source) stored in a [Virtual Authenticator](#virtual-authenticators), regardless of whether they were stored using [Add Credential](#add-credential) or `navigator.credentials.create()`. It is defined as follows:

| HTTP Method | URI Template |
| --- | --- |
| GET | `/session/{session id}/webauthn/authenticator/{authenticatorId}/credentials` |

The [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) are:

1. If authenticatorId does not match any [Virtual Authenticator](#virtual-authenticators) stored in the [Virtual Authenticator Database](#virtual-authenticator-database), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
2. Let credentialsArray be an empty array.
3. For each [Public Key Credential Source](#public-key-credential-source) credential, managed by the authenticator identified by authenticatorId, construct a corresponding [Credential Parameters](#credential-parameters) [Object](https://w3c.github.io/webdriver/#dfn-object) and add it to credentialsArray.
4. Return [success](https://w3c.github.io/webdriver/#dfn-success) with data containing credentialsArray.

### 11.7. Remove Credential

The [Remove Credential](#remove-credential) WebDriver [extension command](https://w3c.github.io/webdriver/#dfn-extension-commands) removes a [Public Key Credential Source](#public-key-credential-source) stored on a [Virtual Authenticator](#virtual-authenticators). It is defined as follows:

| HTTP Method | URI Template |
| --- | --- |
| DELETE | `/session/{session id}/webauthn/authenticator/{authenticatorId}/credentials/{credentialId}` |

The [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) are:

1. If authenticatorId does not match any [Virtual Authenticator](#virtual-authenticators) stored in the [Virtual Authenticator Database](#virtual-authenticator-database), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
2. Let authenticator be the [Virtual Authenticator](#virtual-authenticators) identified by authenticatorId.
3. If credentialId does not match any [Public Key Credential Source](#public-key-credential-source) managed by authenticator, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
4. Remove the [Public Key Credential Source](#public-key-credential-source) identified by credentialId managed by authenticator.
5. Return [success](https://w3c.github.io/webdriver/#dfn-success).

### 11.8. Remove All Credentials

The [Remove All Credentials](#remove-all-credentials) WebDriver [extension command](https://w3c.github.io/webdriver/#dfn-extension-commands) removes all [Public Key Credential Sources](#public-key-credential-source) stored on a [Virtual Authenticator](#virtual-authenticators). It is defined as follows:

| HTTP Method | URI Template |
| --- | --- |
| DELETE | `/session/{session id}/webauthn/authenticator/{authenticatorId}/credentials` |

The [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) are:

1. If authenticatorId does not match any [Virtual Authenticator](#virtual-authenticators) stored in the [Virtual Authenticator Database](#virtual-authenticator-database), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
2. Remove all [Public Key Credential Sources](#public-key-credential-source) managed by the [Virtual Authenticator](#virtual-authenticators) identified by authenticatorId.
3. Return [success](https://w3c.github.io/webdriver/#dfn-success).

### 11.9. Set User Verified

The [Set User Verified](#set-user-verified) [extension command](https://w3c.github.io/webdriver/#dfn-extension-commands) sets the isUserVerified property on the [Virtual Authenticator](#virtual-authenticators). It is defined as follows:

| HTTP Method | URI Template |
| --- | --- |
| POST | `/session/{session id}/webauthn/authenticator/{authenticatorId}/uv` |

The [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) are:

1. If parameters is not a JSON [Object](https://w3c.github.io/webdriver/#dfn-object), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
2. If authenticatorId does not match any [Virtual Authenticator](#virtual-authenticators) stored in the [Virtual Authenticator Database](#virtual-authenticator-database), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
3. If isUserVerified is not a defined property of parameters, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
4. Let authenticator be the [Virtual Authenticator](#virtual-authenticators) identified by authenticatorId.
5. Set the authenticator ’s isUserVerified property to the parameters ’ isUserVerified property.
6. Return [success](https://w3c.github.io/webdriver/#dfn-success).

### 11.10. Set Credential Properties

The [Set Credential Properties](#set-credential-properties) [extension command](https://w3c.github.io/webdriver/#dfn-extension-commands) allows setting the backupEligibility and backupState [credential properties](#credential-properties) of a [Virtual Authenticator](#virtual-authenticators) ’s [public key credential source](#public-key-credential-source). It is defined as follows:

| HTTP Method | URI Template |
| --- | --- |
| POST | `/session/{session id}/webauthn/authenticator/{authenticatorId}/credentials/{credentialId}/props` |

The Set Credential Properties Parameters is a JSON [Object](https://w3c.github.io/webdriver/#dfn-object) passed to the [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) as parameters. It contains the following key and value pairs:

| Key | Description | Value Type |
| --- | --- | --- |
| backupEligibility | The [backup eligibility](#backup-eligibility) [credential property](#credential-properties). | boolean |
| backupState | The [backup state](#backup-state) [credential property](#credential-properties). | boolean |

The [remote end steps](https://w3c.github.io/webdriver/#dfn-remote-end-steps) are:

1. If parameters is not a JSON [Object](https://w3c.github.io/webdriver/#dfn-object), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
	Note: parameters is a [Set Credential Properties Parameters](#set-credential-properties-parameters) object.
2. If authenticatorId does not match any [Virtual Authenticator](#virtual-authenticators) stored in the [Virtual Authenticator Database](#virtual-authenticator-database), return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
3. Let credential be the [public key credential source](#public-key-credential-source) managed by authenticator matched by credentialId.
4. If credential is empty, return a with [invalid argument](https://w3c.github.io/webdriver/#dfn-invalid-argument).
5. Let backupEligibility be the parameters ’ backupEligibility property.
6. If backupEligibility is defined, set the [backup eligibility](#backup-eligibility) [credential property](#credential-properties) of credential to the value of backupEligibility.
	Note: Normally, the backupEligibility property is permanent to a [public key credential source](#public-key-credential-source). [Set Credential Properties](#set-credential-properties) allows changing it for testing and debugging purposes.
7. Let backupState be the parameters ’ backupState property.
8. If backupState is defined, set the [backup state](#backup-state) [credential property](#credential-properties) of credential to the value of backupState.
9. Return [success](https://w3c.github.io/webdriver/#dfn-success).

## 12\. IANA Considerations

### 12.1. WebAuthn Attestation Statement Format Identifier Registrations Updates

This section updates the below-listed attestation statement formats defined in Section [§ 8 Defined Attestation Statement Formats](#sctn-defined-attestation-formats) in the IANA "WebAuthn Attestation Statement Format Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)"), originally registered in [\[WebAuthn-1\]](#biblio-webauthn-1 "Web Authentication:An API for accessing Public Key Credentials Level 1"), to point to this specification.

- WebAuthn Attestation Statement Format Identifier: packed
- Description: The "packed" attestation statement format is a WebAuthn-optimized format for [attestation](#attestation). It uses a very compact but still extensible encoding method. This format is implementable by authenticators with limited resources (e.g., secure elements).
- Specification Document: Section [§ 8.2 Packed Attestation Statement Format](#sctn-packed-attestation) of this specification
- WebAuthn Attestation Statement Format Identifier: tpm
- Description: The TPM attestation statement format returns an attestation statement in the same format as the packed attestation statement format, although the rawData and signature fields are computed differently.
- Specification Document: Section [§ 8.3 TPM Attestation Statement Format](#sctn-tpm-attestation) of this specification
- WebAuthn Attestation Statement Format Identifier: android-key
- Description: [Platform authenticators](#platform-authenticators) on versions "N", and later, may provide this proprietary "hardware attestation" statement.
- Specification Document: Section [§ 8.4 Android Key Attestation Statement Format](#sctn-android-key-attestation) of this specification
- WebAuthn Attestation Statement Format Identifier: android-safetynet
- Description: Android-based [platform authenticators](#platform-authenticators) MAY produce an attestation statement based on the Android SafetyNet API.
- Specification Document: Section [§ 8.5 Android SafetyNet Attestation Statement Format](#sctn-android-safetynet-attestation) of this specification
- WebAuthn Attestation Statement Format Identifier: fido-u2f
- Description: Used with FIDO U2F authenticators
- Specification Document: Section [§ 8.6 FIDO U2F Attestation Statement Format](#sctn-fido-u2f-attestation) of this specification

### 12.2. WebAuthn Attestation Statement Format Identifier Registrations

This section registers the below-listed attestation statement formats, newly defined in Section [§ 8 Defined Attestation Statement Formats](#sctn-defined-attestation-formats), in the IANA "WebAuthn Attestation Statement Format Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)").

- WebAuthn Attestation Statement Format Identifier: apple
- Description: Used with Apple devices' [platform authenticators](#platform-authenticators)
- Specification Document: Section [§ 8.8 Apple Anonymous Attestation Statement Format](#sctn-apple-anonymous-attestation) of this specification
- WebAuthn Attestation Statement Format Identifier: none
- Description: Used to replace any authenticator-provided attestation statement when a [WebAuthn Relying Party](#webauthn-relying-party) indicates it does not wish to receive attestation information.
- Specification Document: Section [§ 8.7 None Attestation Statement Format](#sctn-none-attestation) of this specification

### 12.3. WebAuthn Extension Identifier Registrations Updates

This section updates the below-listed [extension identifier](#extension-identifier) values defined in Section [§ 10 Defined Extensions](#sctn-defined-extensions) in the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)"), originally registered in [\[WebAuthn-1\]](#biblio-webauthn-1 "Web Authentication:An API for accessing Public Key Credentials Level 1"), to point to this specification.

- WebAuthn Extension Identifier: appid
- Description: This [authentication extension](#authentication-extension) allows [WebAuthn Relying Parties](#webauthn-relying-party) that have previously registered a credential using the legacy FIDO JavaScript APIs to request an assertion.
- Specification Document: Section [§ 10.1.1 FIDO AppID Extension (appid)](#sctn-appid-extension) of this specification

### 12.4. WebAuthn Extension Identifier Registrations

This section registers the below-listed [extension identifier](#extension-identifier) values, newly defined in Section [§ 10 Defined Extensions](#sctn-defined-extensions), in the IANA "WebAuthn Extension Identifiers" registry [\[IANA-WebAuthn-Registries\]](#biblio-iana-webauthn-registries "Web Authentication (WebAuthn) registries") established by [\[RFC8809\]](#biblio-rfc8809 "Registries for Web Authentication (WebAuthn)").

- WebAuthn Extension Identifier: appidExclude
- Description: This registration extension allows [WebAuthn Relying Parties](#webauthn-relying-party) to exclude authenticators that contain specified credentials that were created with the legacy FIDO U2F JavaScript API [\[FIDOU2FJavaScriptAPI\]](#biblio-fidou2fjavascriptapi "FIDO U2F JavaScript API").
- Specification Document: Section [§ 10.1.2 FIDO AppID Exclusion Extension (appidExclude)](#sctn-appid-exclude-extension) of this specification
- WebAuthn Extension Identifier: credProps
- Description: This [client](#client-extension) [registration extension](#registration-extension) enables reporting of a newly-created [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) ’s properties, as determined by the [client](#client), to the calling [WebAuthn Relying Party](#webauthn-relying-party) ’s [web application](#web-application).
- Specification Document: Section [§ 10.1.3 Credential Properties Extension (credProps)](#sctn-authenticator-credential-properties-extension) of this specification
- WebAuthn Extension Identifier: largeBlob
- Description: This [client](#client-extension) [registration extension](#registration-extension) and [authentication extension](#authentication-extension) allows a [Relying Party](#relying-party) to store opaque data associated with a credential.
- Specification Document: Section [§ 10.1.5 Large blob storage extension (largeBlob)](#sctn-large-blob-extension) of this specification

## 13\. Security Considerations

This specification defines a [Web API](#sctn-api) and a cryptographic peer-entity authentication protocol. The [Web Authentication API](#web-authentication-api) allows Web developers (i.e., "authors") to utilize the Web Authentication protocol in their [registration](#registration) and [authentication](#authentication) [ceremonies](#ceremony). The entities comprising the Web Authentication protocol endpoints are user-controlled [WebAuthn Authenticators](#webauthn-authenticator) and a [WebAuthn Relying Party](#webauthn-relying-party) ’s computing environment hosting the [Relying Party](#relying-party) ’s [web application](#web-application). In this model, the user agent, together with the [WebAuthn Client](#webauthn-client), comprise an intermediary between [authenticators](#authenticator) and [Relying Parties](#relying-party). Additionally, [authenticators](#authenticator) can [attest](#attestation) to [Relying Parties](#relying-party) as to their provenance.

At this time, this specification does not feature detailed security considerations. However, the [\[FIDOSecRef\]](#biblio-fidosecref "FIDO Security Reference") document provides a security analysis which is overall applicable to this specification. Also, the [\[FIDOAuthnrSecReqs\]](#biblio-fidoauthnrsecreqs "FIDO Authenticator Security Requirements") document suite provides useful information about [authenticator](#authenticator) security characteristics.

The below subsections comprise the current Web Authentication-specific security considerations. They are divided by audience; general security considerations are direct subsections of this section, while security considerations specifically for [authenticator](#authenticator), [client](#client) and [Relying Party](#relying-party) implementers are grouped into respective subsections.

### 13.1. Credential ID Unsigned

The [credential ID](#credential-id) accompanying an [authentication assertion](#authentication-assertion) is not signed. This is not a problem because all that would happen if an [authenticator](#authenticator) returns the wrong [credential ID](#credential-id), or if an attacker intercepts and manipulates the [credential ID](#credential-id), is that the [WebAuthn Relying Party](#webauthn-relying-party) would not look up the correct [credential public key](#credential-public-key) with which to verify the returned signed [authenticator data](#authenticator-data) (a.k.a., [assertion](#assertion)), and thus the interaction would end in an error.

### 13.2. Physical Proximity between Client and Authenticator

In the WebAuthn [authenticator model](#authenticator-model), it is generally assumed that [roaming authenticators](#roaming-authenticators) are physically close to, and communicate directly with, the [client](#client). This arrangement has some important advantages.

The promise of physical proximity between [client](#client) and [authenticator](#authenticator) is a key strength of a [something you have](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) [authentication factor](https://pages.nist.gov/800-63-3/sp800-63-3.html#af). For example, if a [roaming authenticator](#roaming-authenticators) can communicate only via USB or Bluetooth, the limited range of these transports ensures that any malicious actor must physically be within that range in order to interact with the [authenticator](#authenticator). This is not necessarily true of an [authenticator](#authenticator) that can be invoked remotely — even if the [authenticator](#authenticator) verifies [user presence](#concept-user-present), users can be tricked into authorizing remotely initiated malicious requests.

Direct communication between [client](#client) and [authenticator](#authenticator) means the [client](#client) can enforce the [scope](#scope) restrictions for [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential). By contrast, if the communication between [client](#client) and [authenticator](#authenticator) is mediated by some third party, then the [client](#client) has to trust the third party to enforce the [scope](#scope) restrictions and control access to the [authenticator](#authenticator). Failure to do either could result in a malicious [Relying Party](#relying-party) receiving [authentication assertions](#authentication-assertion) valid for other [Relying Parties](#relying-party), or in a malicious user gaining access to [authentication assertions](#authentication-assertion) for other users.

If designing a solution where the [authenticator](#authenticator) does not need to be physically close to the [client](#client), or where [client](#client) and [authenticator](#authenticator) do not communicate directly, designers SHOULD consider how this affects the enforcement of [scope](#scope) restrictions and the strength of the [authenticator](#authenticator) as a [something you have](https://pages.nist.gov/800-63-3/sp800-63-3.html#af) authentication factor.

### 13.3.

#### 13.3.1. Attestation Certificate Hierarchy

A 3-tier hierarchy for attestation certificates is RECOMMENDED (i.e., Attestation Root, Attestation Issuing CA, Attestation Certificate). It is also RECOMMENDED that for each [WebAuthn Authenticator](#webauthn-authenticator) device line (i.e., model), a separate issuing CA is used to help facilitate isolating problems with a specific version of an authenticator model.

If the attestation root certificate is not dedicated to a single [WebAuthn Authenticator](#webauthn-authenticator) device line (i.e., AAGUID), the AAGUID SHOULD be specified in the attestation certificate itself, so that it can be verified against the [authenticator data](#authenticator-data).

#### 13.3.2. Attestation Certificate and Attestation Certificate CA Compromise

When an intermediate CA or a root CA used for issuing attestation certificates is compromised, [WebAuthn Authenticator](#webauthn-authenticator) [attestation key pairs](#attestation-key-pair) are still safe although their certificates can no longer be trusted. A [WebAuthn Authenticator](#webauthn-authenticator) manufacturer that has recorded the [attestation public keys](#attestation-public-key) for their [authenticator](#authenticator) models can issue new [attestation certificates](#attestation-certificate) for these keys from a new intermediate CA or from a new root CA. If the root CA changes, the [WebAuthn Relying Parties](#webauthn-relying-party) MUST update their trusted root certificates accordingly.

A [WebAuthn Authenticator](#webauthn-authenticator) [attestation certificate](#attestation-certificate) MUST be revoked by the issuing CA if its [private key](#attestation-private-key) has been compromised. A WebAuthn Authenticator manufacturer may need to ship a firmware update and inject new [attestation private keys](#attestation-private-key) and [certificates](#attestation-certificate) into already manufactured [WebAuthn Authenticators](#webauthn-authenticator), if the exposure was due to a firmware flaw. (The process by which this happens is out of scope for this specification.) If the [WebAuthn Authenticator](#webauthn-authenticator) manufacturer does not have this capability, then it may not be possible for [Relying Parties](#relying-party) to trust any further [attestation statements](#attestation-statement) from the affected [WebAuthn Authenticators](#webauthn-authenticator).

See also the related security consideration for [Relying Parties](#relying-party) in [§ 13.4.5 Revoked Attestation Certificates](#sctn-revoked-attestation-certificates).

### 13.4.

#### 13.4.1. Security Benefits for WebAuthn Relying Parties

The main benefits offered to [WebAuthn Relying Parties](#webauthn-relying-party) by this specification include:

1. Users and accounts can be secured using widely compatible, easy-to-use multi-factor authentication.
2. The [Relying Party](#relying-party) does not need to provision [authenticator](#authenticator) hardware to its users. Instead, each user can independently obtain any conforming [authenticator](#authenticator) and use that same [authenticator](#authenticator) with any number of [Relying Parties](#relying-party). The [Relying Party](#relying-party) can optionally enforce requirements on [authenticators](#authenticator) ' security properties by inspecting the [attestation statements](#attestation-statement) returned from the [authenticators](#authenticator).
3. [Authentication ceremonies](#authentication-ceremony) are resistant to [man-in-the-middle attacks](https://tools.ietf.org/html/rfc4949#page-186). Regarding [registration ceremonies](#registration-ceremony), see [§ 13.4.4 Attestation Limitations](#sctn-attestation-limitations), below.
4. The [Relying Party](#relying-party) can automatically support multiple types of [user verification](#user-verification) - for example PIN, biometrics and/or future methods - with little or no code change, and can let each user decide which they prefer to use via their choice of [authenticator](#authenticator).
5. The [Relying Party](#relying-party) does not need to store additional secrets in order to gain the above benefits.

As stated in the [Conformance](#sctn-conforming-relying-parties) section, the [Relying Party](#relying-party) MUST behave as described in [§ 7 WebAuthn Relying Party Operations](#sctn-rp-operations) to obtain all of the above security benefits. However, one notable use case that departs slightly from this is described below in [§ 13.4.4 Attestation Limitations](#sctn-attestation-limitations).

#### 13.4.2. Visibility Considerations for Embedded Usage

Simplistic use of WebAuthn in an embedded context, e.g., within `iframe` s as described in [§ 5.10 Using Web Authentication within iframe elements](#sctn-iframe-guidance), may make users vulnerable to UI Redressing attacks, also known as " [Clickjacking](https://en.wikipedia.org/wiki/Clickjacking) ". This is where an attacker overlays their own UI on top of a [Relying Party](#relying-party) ’s intended UI and attempts to trick the user into performing unintended actions with the [Relying Party](#relying-party). For example, using these techniques, an attacker might be able to trick users into purchasing items, transferring money, etc.

Even though WebAuthn-specific UI is typically handled by the [client platform](#client-platform) and thus is not vulnerable to [UI Redressing](#ui-redressing), it is likely important for an [Relying Party](#relying-party) having embedded WebAuthn-wielding content to ensure that their content’s UI is visible to the user. An emerging means to do so is by observing the status of the experimental [Intersection Observer v2](https://w3c.github.io/IntersectionObserver/v2/) ’s `isVisible` attribute. For example, the [Relying Party](#relying-party) ’s script running in the embedded context could pre-emptively load itself in a popup window if it detects `isVisble` being set to `false`, thus side-stepping any occlusion of their content.

#### 13.4.3. Cryptographic Challenges

As a cryptographic protocol, Web Authentication is dependent upon randomized challenges to avoid replay attacks. Therefore, the values of both `PublicKeyCredentialCreationOptions`.`challenge` and `PublicKeyCredentialRequestOptions`.`challenge` MUST be randomly generated by [Relying Parties](#relying-party) in an environment they trust (e.g., on the server-side), and the returned `challenge` value in the client’s response MUST match what was generated. This SHOULD be done in a fashion that does not rely upon a client’s behavior, e.g., the [Relying Party](#relying-party) SHOULD store the challenge temporarily until the operation is complete. Tolerating a mismatch will compromise the security of the protocol.

Challenges SHOULD be valid for a duration similar to the upper limit of the.

In order to prevent replay attacks, the challenges MUST contain enough entropy to make guessing them infeasible. Challenges SHOULD therefore be at least 16 bytes long.

#### 13.4.4. Attestation Limitations

*This section is not normative.*

When [registering a new credential](#sctn-registering-a-new-credential), the [attestation statement](#attestation-statement), if present, may allow the [WebAuthn Relying Party](#webauthn-relying-party) to derive assurances about various [authenticator](#authenticator) qualities. For example, the [authenticator](#authenticator) model, or how it stores and protects [credential private keys](#credential-private-key). However, it is important to note that an [attestation statement](#attestation-statement), on its own, provides no means for a [Relying Party](#relying-party) to verify that an [attestation object](#attestation-object) was generated by the [authenticator](#authenticator) the user intended, and not by a [man-in-the-middle attacker](https://tools.ietf.org/html/rfc4949#page-186). For example, such an attacker could use malicious code injected into [Relying Party](#relying-party) script. The [Relying Party](#relying-party) must therefore rely on other means, e.g., TLS and related technologies, to protect the [attestation object](#attestation-object) from [man-in-the-middle attacks](https://tools.ietf.org/html/rfc4949#page-186).

Under the assumption that a [registration ceremony](#registration-ceremony) is completed securely, and that the [authenticator](#authenticator) maintains confidentiality of the [credential private key](#credential-private-key), subsequent [authentication ceremonies](#authentication-ceremony) using that [public key credential](#public-key-credential) are resistant to tampering by [man-in-the-middle attacks](https://tools.ietf.org/html/rfc4949#page-186).

The discussion above holds for all [attestation types](#attestation-type). In all cases it is possible for a [man-in-the-middle attacker](https://tools.ietf.org/html/rfc4949#page-186) to replace the `PublicKeyCredential` object, including the [attestation statement](#attestation-statement) and the [credential public key](#credential-public-key) to be registered, and subsequently tamper with future [authentication assertions](#authentication-assertion) [scoped](#scope) for the same [Relying Party](#relying-party) and passing through the same attacker.

Such an attack would potentially be detectable; since the [Relying Party](#relying-party) has registered the attacker’s [credential public key](#credential-public-key) rather than the user’s, the attacker must tamper with all subsequent [authentication ceremonies](#authentication-ceremony) with that [Relying Party](#relying-party): unscathed ceremonies will fail, potentially revealing the attack.

[Attestation types](#attestation-type) other than [Self Attestation](#self-attestation) and [None](#none) can increase the difficulty of such attacks, since [Relying Parties](#relying-party) can possibly display [authenticator](#authenticator) information, e.g., model designation, to the user. An attacker might therefore need to use a genuine [authenticator](#authenticator) of the same model as the user’s [authenticator](#authenticator), or the user might notice that the [Relying Party](#relying-party) reports a different [authenticator](#authenticator) model than the user expects.

Note: All variants of [man-in-the-middle attacks](https://tools.ietf.org/html/rfc4949#page-186) described above are more difficult for an attacker to mount than a [man-in-the-middle attack](https://tools.ietf.org/html/rfc4949#page-186) against conventional password authentication.

#### 13.4.5. Revoked Attestation Certificates

If [attestation certificate](#attestation-certificate) validation fails due to a revoked intermediate attestation CA certificate, and the [Relying Party](#relying-party) ’s policy requires rejecting the registration/authentication request in these situations, then it is RECOMMENDED that the [Relying Party](#relying-party) also un-registers (or marks with a trust level equivalent to " [self attestation](#self-attestation) ") [public key credentials](#public-key-credential) that were registered after the CA compromise date using an [attestation certificate](#attestation-certificate) chaining up to the same intermediate CA. It is thus RECOMMENDED that [Relying Parties](#relying-party) remember intermediate attestation CA certificates during [registration](#registration) in order to un-register related [public key credentials](#public-key-credential) if the [registration](#registration) was performed after revocation of such certificates.

See also the related security consideration for [authenticators](#authenticator) in [§ 13.3.2 Attestation Certificate and Attestation Certificate CA Compromise](#sctn-ca-compromise).

#### 13.4.6. Credential Loss and Key Mobility

This specification defines no protocol for backing up [credential private keys](#credential-private-key), or for sharing them between [authenticators](#authenticator). In general, it is expected that a [credential private key](#credential-private-key) never leaves the [authenticator](#authenticator) that created it. Losing an [authenticator](#authenticator) therefore, in general, means losing all [credentials](#public-key-credential) [bound](#bound-credential) to the lost [authenticator](#authenticator), which could lock the user out of an account if the user has only one [credential](#public-key-credential) registered with the [Relying Party](#relying-party). Instead of backing up or sharing private keys, the Web Authentication API allows registering multiple [credentials](#public-key-credential) for the same user. For example, a user might register [platform credentials](#platform-credential) on frequently used [client devices](#client-device), and one or more [roaming credentials](#roaming-credential) for use as backup and with new or rarely used [client devices](#client-device).

[Relying Parties](#relying-party) SHOULD allow and encourage users to register multiple [credentials](#public-key-credential) to the same [user account](#user-account). [Relying Parties](#relying-party) SHOULD make use of the `` `excludeCredentials` `` and `` `user`.`id` `` options to ensure that these different [credentials](#public-key-credential) are [bound](#bound-credential) to different [authenticators](#authenticator).

#### 13.4.7. Unprotected account detection

*This section is not normative.*

This security consideration applies to [Relying Parties](#relying-party) that support [authentication ceremonies](#authentication-ceremony) with a non- [empty](https://infra.spec.whatwg.org/#list-empty) `allowCredentials` argument as the first authentication step. For example, if using authentication with [server-side credentials](#server-side-credential) as the first authentication step.

In this case the `allowCredentials` argument risks leaking information about which [user accounts](#user-account) have WebAuthn credentials registered and which do not, which may be a signal of account protection strength. For example, say an attacker can initiate an [authentication ceremony](#authentication-ceremony) by providing only a username, and the [Relying Party](#relying-party) responds with a non-empty `allowCredentials` for some [user accounts](#user-account), and with failure or a password challenge for other [user accounts](#user-account). The attacker can then conclude that the latter [user accounts](#user-account) likely do not require a WebAuthn [assertion](#assertion) for successful authentication, and thus focus an attack on those likely weaker accounts.

This issue is similar to the one described in [§ 14.6.2 Username Enumeration](#sctn-username-enumeration) and [§ 14.6.3 Privacy leak via credential IDs](#sctn-credential-id-privacy-leak), and can be mitigated in similar ways.

#### 13.4.8. Code injection attacks

Any malicious code executing on an [origin](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) within the [scope](#scope) of a [Relying Party](#relying-party) ’s [public key credentials](#public-key-credential) has the potential to invalidate any and all security guarantees WebAuthn may provide. [WebAuthn Clients](#webauthn-client) only expose the WebAuthn API in [secure contexts](https://html.spec.whatwg.org/multipage/webappapis.html#secure-context), which mitigates the most basic attacks but SHOULD be combined with additional precautions by [Relying Parties](#relying-party).

Code injection can happen in several ways; this section attempts to point out some likely scenarios and suggest suitable mitigations, but is not an exhaustive list.

- Malicous code could be injected by a third-party script included by the [Relying Party](#relying-party), either intentionally or due to a security vulnerability in the third party.
	The [Relying Party](#relying-party) therefore SHOULD limit the amount of third-party script included on the [origins](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) within the [scope](#scope) of its [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential).
	The [Relying Party](#relying-party) SHOULD use Content Security Policy [\[CSP2\]](#biblio-csp2 "Content Security Policy Level 2"), and/or other appropriate technologies available at the time, to limit what script can run on its [origins](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised).
- Malicious code could, by the credential [scope](#scope) rules, be hosted on a subdomain of the [RP ID](#rp-id). For example, user-submitted code hosted on `usercontent.example.org` could exercise any [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential) [scoped](#scope) to the [RP ID](#rp-id) `example.org`. If the [Relying Party](#relying-party) allows a subdomain `origin` when [verifying the assertion](#rp-op-verifying-assertion-step-origin), malicious users could use this to launch a [man-in-the-middle attack](https://tools.ietf.org/html/rfc4949#page-186) to obtain valid [authentication assertions](#authentication-assertion) and impersonate the victims of the attack.
	Therefore, the [Relying Party](#relying-party) by default SHOULD NOT allow a subdomain `origin` when [verifying the assertion](#rp-op-verifying-assertion-step-origin). If the [Relying Party](#relying-party) needs to allow a subdomain `origin`, then the [Relying Party](#relying-party) MUST NOT serve untrusted code on any allowed subdomain of [origins](#determines-the-set-of-origins-on-which-the-public-key-credential-may-be-exercised) within the [scope](#scope) of its [public key credentials](#public-key-credential).

#### 13.4.9. Validating the origin of a credential

When [registering a credential](#rp-op-registering-a-new-credential-step-origin) and when [verifying an assertion](#rp-op-verifying-assertion-step-origin), the [Relying Party](#relying-party) MUST validate the `origin` member of the [client data](#client-data).

The [Relying Party](#relying-party) MUST NOT accept unexpected values of `origin`, as doing so could allow a malicious website to obtain valid [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential). Although the [scope](#scope) of WebAuthn credentials prevents their use on domains outside the [RP ID](#rp-id) they were registered for, the [Relying Party](#relying-party) ’s origin validation serves as an additional layer of protection in case a faulty [authenticator](#authenticator) fails to enforce credential [scope](#scope). See also [§ 13.4.8 Code injection attacks](#sctn-code-injection) for discussion of potentially malicious subdomains.

Validation MAY be performed by exact string matching or any other method as needed by the [Relying Party](#relying-party). For example:

- A web application served only at `https://example.org` SHOULD require `origin` to exactly equal `https://example.org`.
	This is the simplest case, where `origin` is expected to be the string `https://` followed by the [RP ID](#rp-id).
- A web application served at a small number of domains might require `origin` to exactly equal some element of a list of allowed origins, for example the list `["https://example.org", "https://login.example.org"]`.
- A web application leveraging [related origin requests](#sctn-related-origins) might also require `origin` to exactly equal some element of a list of allowed origins, for example the list `["https://example.co.uk", "https://example.de", "https://myexamplerewards.com"]`. This list will typically match the origins listed in the well-known URI for the [RP ID](#rp-id). See [§ 5.11 Using Web Authentication across related origins](#sctn-related-origins).
- A web application served at a large set of domains that changes often might parse `origin` structurally and require that the URL scheme is `https` and that the authority equals or is any subdomain of the [RP ID](#rp-id) - for example, `example.org` or any subdomain of `example.org`).
	Note: See [§ 13.4.8 Code injection attacks](#sctn-code-injection) for a discussion of the risks of allowing any subdomain of the [RP ID](#rp-id).
- A web application with a companion native application might allow `origin` to be an operating system dependent identifier for the native application. For example, such a [Relying Party](#relying-party) might require that `origin` exactly equals some element of the list `["https://example.org", "example-os:appid:204ffa1a5af110ac483f131a1bef8a841a7adb0d8d135908bbd964ed05d2653b"]`.

Similar considerations apply when validating the `topOrigin` member of the [client data](#client-data). When `topOrigin` is present, the [Relying Party](#relying-party) MUST validate that its value is expected. This validation MAY be performed by exact string matching or any other method as needed by the [Relying Party](#relying-party). For example:

- A web application that does not wish to be embedded in a cross-origin `iframe` might require `topOrigin` to exactly equal `origin`.
- A web application that wishes to be embedded in a cross-origin `iframe` on a small number of domains might require `topOrigin` to exactly equal some element of a list of allowed origins, for example the list `["https://example-partner1.org", "https://login.partner2-example.org"]`.
- A web application that wishes to be embedded in a cross-origin `iframe` on a large number of domains might allow any value of `topOrigin`, or use a dynamic procedure to determine whether a given `topOrigin` value is allowed for a particular ceremony.

## 14\. Privacy Considerations

The privacy principles in [\[FIDO-Privacy-Principles\]](#biblio-fido-privacy-principles "FIDO Privacy Principles") also apply to this specification.

This section is divided by audience; general privacy considerations are direct subsections of this section, while privacy considerations specifically for [authenticator](#authenticator), [client](#client) and [Relying Party](#relying-party) implementers are grouped into respective subsections.

### 14.1. De-anonymization Prevention Measures

*This section is not normative.*

Many aspects of the design of the [Web Authentication API](#web-authentication-api) are motivated by privacy concerns. The main concern considered in this specification is the protection of the user’s personal identity, i.e., the identification of a human being or a correlation of separate identities as belonging to the same human being. Although the [Web Authentication API](#web-authentication-api) does not use or provide any form of global identity, the following kinds of potentially correlatable identifiers are used:

- The user’s [credential IDs](#credential-id) and [credential public keys](#credential-public-key).
	These are registered by the [WebAuthn Relying Party](#webauthn-relying-party) and subsequently used by the user to prove possession of the corresponding [credential private key](#credential-private-key). They are also visible to the [client](#client) in the communication with the [authenticator](#authenticator).
- The user’s identities specific to each [Relying Party](#relying-party), e.g., usernames and [user handles](#user-handle).
	These identities are obviously used by each [Relying Party](#relying-party) to identify a user in their system. They are also visible to the [client](#client) in the communication with the [authenticator](#authenticator).
- The user’s biometric characteristic(s), e.g., fingerprints or facial recognition data [\[ISOBiometricVocabulary\]](#biblio-isobiometricvocabulary "Information technology — Vocabulary — Biometrics").
	This is optionally used by the [authenticator](#authenticator) to perform [user verification](#user-verification). It is not revealed to the [Relying Party](#relying-party), but in the case of [platform authenticators](#platform-authenticators), it might be visible to the [client](#client) depending on the implementation.
- The models of the user’s [authenticators](#authenticator), e.g., product names.
	This is exposed in the [attestation statement](#attestation-statement) provided to the [Relying Party](#relying-party) during [registration](#registration). It is also visible to the [client](#client) in the communication with the [authenticator](#authenticator).
- The identities of the user’s [authenticators](#authenticator), e.g., serial numbers.
	This is possibly used by the [client](#client) to enable communication with the [authenticator](#authenticator), but is not exposed to the [Relying Party](#relying-party).

Some of the above information is necessarily shared with the [Relying Party](#relying-party). The following sections describe the measures taken to prevent malicious [Relying Parties](#relying-party) from using it to discover a user’s personal identity.

### 14.2.

*This section is not normative.*

Although [Credential IDs](#credential-id) and [credential public keys](#credential-public-key) are necessarily shared with the [WebAuthn Relying Party](#webauthn-relying-party) to enable strong authentication, they are designed to be minimally identifying and not shared between [Relying Parties](#relying-party).

- [Credential IDs](#credential-id) and [credential public keys](#credential-public-key) are meaningless in isolation, as they only identify [credential key pairs](#credential-key-pair) and not users directly.
- Each [public key credential](#public-key-credential) is strictly [scoped](#scope) to a specific [Relying Party](#relying-party), and the [client](#client) ensures that its existence is not revealed to other [Relying Parties](#relying-party). A malicious [Relying Party](#relying-party) thus cannot ask the [client](#client) to reveal a user’s other identities.
- The [client](#client) also ensures that the existence of a [public key credential](#public-key-credential) is not revealed to the [Relying Party](#relying-party) without. This is detailed further in [§ 14.5.1 Registration Ceremony Privacy](#sctn-make-credential-privacy) and [§ 14.5.2 Authentication Ceremony Privacy](#sctn-assertion-privacy). A malicious [Relying Party](#relying-party) thus cannot silently identify a user, even if the user has a [public key credential](#public-key-credential) registered and available.
- [Authenticators](#authenticator) ensure that the [credential IDs](#credential-id) and [credential public keys](#credential-public-key) of different [public key credentials](#public-key-credential) are not correlatable as belonging to the same user. A pair of malicious [Relying Parties](#relying-party) thus cannot correlate users between their systems without additional information, e.g., a willfully reused username or e-mail address.
- [Authenticators](#authenticator) ensure that their [attestation certificates](#attestation-certificate) are not unique enough to identify a single [authenticator](#authenticator) or a small group of [authenticators](#authenticator). This is detailed further in [§ 14.4.1 Attestation Privacy](#sctn-attestation-privacy). A pair of malicious [Relying Parties](#relying-party) thus cannot correlate users between their systems by tracking individual [authenticators](#authenticator).

Additionally, a [client-side discoverable public key credential source](#client-side-discoverable-public-key-credential-source) can optionally include a [user handle](#user-handle) specified by the [Relying Party](#relying-party). The [credential](#public-key-credential) can then be used to both identify and [authenticate](#authentication) the user. This means that a privacy-conscious [Relying Party](#relying-party) can allow creation of a [user account](#user-account) without a traditional username, further improving non-correlatability between [Relying Parties](#relying-party).

### 14.3.

[Biometric authenticators](#biometric-authenticator) perform the [biometric recognition](#biometric-recognition) internally in the [authenticator](#authenticator) - though for [platform authenticators](#platform-authenticators) the biometric data might also be visible to the [client](#client), depending on the implementation. Biometric data is not revealed to the [WebAuthn Relying Party](#webauthn-relying-party); it is used only locally to perform [user verification](#user-verification) authorizing the creation and [registration](#registration) of, or [authentication](#authentication) using, a [public key credential](#public-key-credential). A malicious [Relying Party](#relying-party) therefore cannot discover the user’s personal identity via biometric data, and a security breach at a [Relying Party](#relying-party) cannot expose biometric data for an attacker to use for forging logins at other [Relying Parties](#relying-party).

In the case where a [Relying Party](#relying-party) requires [biometric recognition](#biometric-recognition), this is performed locally by the [biometric authenticator](#biometric-authenticator) perfoming [user verification](#user-verification) and then signaling the result by setting the [UV](#authdata-flags-uv) [flag](#authdata-flags) in the signed [assertion](#assertion) response, instead of revealing the biometric data itself to the [Relying Party](#relying-party).

### 14.4.

#### 14.4.1. Attestation Privacy

[Attestation certificates](#attestation-certificate) and [attestation key pairs](#attestation-key-pair) can be used to track users or link various online identities of the same user together. This can be mitigated in several ways, including:

- A [WebAuthn Authenticator](#webauthn-authenticator) manufacturer may choose to ship [authenticators](#authenticator) in batches where [authenticators](#authenticator) in a batch share the same [attestation certificate](#attestation-certificate) (called [Basic Attestation](#basic-attestation) or [batch attestation](#batch-attestation)). This will anonymize the user at the risk of not being able to revoke a particular [attestation certificate](#attestation-certificate) if its [private key](#attestation-private-key) is compromised. The [authenticator](#authenticator) manufacturer SHOULD then ensure that such batches are large enough to provide meaningful anonymization, while also minimizing the batch size in order to limit the number of affected users in case an [attestation private key](#attestation-private-key) is compromised.
	[\[UAFProtocol\]](#biblio-uafprotocol "FIDO UAF Protocol Specification v1.0") requires that at least 100,000 [authenticator](#authenticator) devices share the same [attestation certificate](#attestation-certificate) in order to produce sufficiently large groups. This may serve as guidance about suitable batch sizes.
- A [WebAuthn Authenticator](#webauthn-authenticator) may be capable of dynamically generating different [attestation key pairs](#attestation-key-pair) (and requesting related [certificates](#attestation-certificate)) per- [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) as described in the [Anonymization CA](#anonymization-ca) approach. For example, an [authenticator](#authenticator) can ship with a main [attestation private key](#attestation-private-key) (and [certificate](#attestation-certificate)), and combined with a cloud-operated [Anonymization CA](#anonymization-ca), can dynamically generate per- [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) [attestation key pairs](#attestation-key-pair) and [attestation certificates](#attestation-certificate).
	Note: In various places outside this specification, the term "Privacy CA" is used to refer to what is termed here as an [Anonymization CA](#anonymization-ca). Because the Trusted Computing Group (TCG) also used the term "Privacy CA" to refer to what the TCG now refers to as an [Attestation CA](#attestation-ca) (ACA) [\[TCG-CMCProfile-AIKCertEnroll\]](#biblio-tcg-cmcprofile-aikcertenroll "TCG Infrastructure Working Group: A CMC Profile for AIK Certificate Enrollment"), we are using the term [Anonymization CA](#anonymization-ca) here to try to mitigate confusion in the specific context of this specification.

#### 14.4.2. Privacy of personally identifying information Stored in Authenticators

[Authenticators](#authenticator) MAY provide additional information to [clients](#client) outside what’s defined by this specification, e.g., to enable the [client](#client) to provide a rich UI with which the user can pick which [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential) to use for an [authentication ceremony](#authentication-ceremony). If an [authenticator](#authenticator) chooses to do so, it SHOULD NOT expose personally identifying information unless successful [user verification](#user-verification) has been performed. If the [authenticator](#authenticator) supports [user verification](#user-verification) with more than one concurrently enrolled user, the [authenticator](#authenticator) SHOULD NOT expose personally identifying information of users other than the currently [verified](#concept-user-verified) user. Consequently, an [authenticator](#authenticator) that is not capable of [user verification](#user-verification) SHOULD NOT store personally identifying information.

For the purposes of this discussion, the [user handle](#user-handle) conveyed as the `id` member of `PublicKeyCredentialUserEntity` is not considered personally identifying information; see [§ 14.6.1 User Handle Contents](#sctn-user-handle-privacy).

These recommendations serve to prevent an adversary with physical access to an [authenticator](#authenticator) from extracting personally identifying information about the [authenticator](#authenticator) ’s enrolled user(s).

### 14.5.

#### 14.5.1. Registration Ceremony Privacy

In order to protect users from being identified without, implementations of the `[[Create]](origin, options, sameOriginWithAncestors)` method need to take care to not leak information that could enable a malicious [WebAuthn Relying Party](#webauthn-relying-party) to distinguish between these cases, where "excluded" means that at least one of the [credentials](#public-key-credential) listed by the [Relying Party](#relying-party) in `excludeCredentials` is [bound](#bound-credential) to the [authenticator](#authenticator):

- No [authenticators](#authenticator) are present.
- At least one [authenticator](#authenticator) is present, and at least one present [authenticator](#authenticator) is excluded.

If the above cases are distinguishable, information is leaked by which a malicious [Relying Party](#relying-party) could identify the user by probing for which [credentials](#public-key-credential) are available. For example, one such information leak is if the client returns a failure response as soon as an excluded [authenticator](#authenticator) becomes available. In this case - especially if the excluded [authenticator](#authenticator) is a [platform authenticator](#platform-authenticators) - the [Relying Party](#relying-party) could detect that the [ceremony](#ceremony) was canceled before the user could feasibly have canceled it manually, and thus conclude that at least one of the [credentials](#public-key-credential) listed in the `excludeCredentials` parameter is available to the user.

The above is not a concern, however, if the user has to create a new credential before a distinguishable error is returned, because in this case the user has confirmed intent to share the information that would be leaked.

#### 14.5.2. Authentication Ceremony Privacy

In order to protect users from being identified without, implementations of the `[[DiscoverFromExternalSource]](origin, options, sameOriginWithAncestors)` method need to take care to not leak information that could enable a malicious [WebAuthn Relying Party](#webauthn-relying-party) to distinguish between these cases, where "named" means that the [credential](#public-key-credential) is listed by the [Relying Party](#relying-party) in `allowCredentials`:

- A named [credential](#public-key-credential) is not available.
- A named [credential](#public-key-credential) is available, but the user does not to use it.

If the above cases are distinguishable, information is leaked by which a malicious [Relying Party](#relying-party) could identify the user by probing for which [credentials](#public-key-credential) are available. For example, one such information leak may happen if the client displays instructions and controls for canceling or proceeding with the [authentication ceremony](#authentication-ceremony) only after discovering an [authenticator](#authenticator) that [contains](#contains) a named [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential). In this case, if the [Relying Party](#relying-party) is aware of this [client](#client) behavior, the [Relying Party](#relying-party) could detect that the [ceremony](#ceremony) was canceled by the user and not the timeout, and thus conclude that at least one of the [credentials](#public-key-credential) listed in the `allowCredentials` parameter is available to the user.

This concern may be addressed by displaying controls allowing the user to cancel an [authentication ceremony](#authentication-ceremony) at any time, regardless of whether any named [credentials](https://w3c.github.io/webappsec-credential-management/#concept-credential) are available.

#### 14.5.3. Privacy Between Operating System Accounts

If a [platform authenticator](#platform-authenticators) is included in a [client device](#client-device) with a multi-user operating system, the [platform authenticator](#platform-authenticators) and [client device](#client-device) SHOULD work together to ensure that the existence of any [platform credential](#platform-credential) is revealed only to the operating system user that created that [platform credential](#platform-credential).

#### 14.5.4. Disclosing Client Capabilities

The `getClientCapabilities` method assists [WebAuthn Relying Parties](#webauthn-relying-party) in crafting registration and authentication experiences which have a high chance of success with the client and/or user.

The client’s support or lack of support of a WebAuthn capability may pose a fingerprinting risk. Client implementations MAY wish to limit capability disclosures based on client policy and/or user consent.

### 14.6.

#### 14.6.1. User Handle Contents

Since the [user handle](#user-handle) is not considered personally identifying information in [§ 14.4.2 Privacy of personally identifying information Stored in Authenticators](#sctn-pii-privacy), and since [authenticators](#authenticator) MAY reveal [user handles](#user-handle) without first performing [user verification](#user-verification), the [Relying Party](#relying-party) MUST NOT include personally identifying information, e.g., e-mail addresses or usernames, in the [user handle](#user-handle). This includes hash values of personally identifying information, unless the hash function is [salted](https://tools.ietf.org/html/rfc4949#page-258) with [salt](https://tools.ietf.org/html/rfc4949#page-258) values private to the [Relying Party](#relying-party), since hashing does not prevent probing for guessable input values. It is RECOMMENDED to let the [user handle](#user-handle) be 64 random bytes, and store this value in the [user account](#user-account).

#### 14.6.2. Username Enumeration

While initiating a [registration](#registration-ceremony) or [authentication ceremony](#authentication-ceremony), there is a risk that the [WebAuthn Relying Party](#webauthn-relying-party) might leak sensitive information about its registered users. For example, if a [Relying Party](#relying-party) uses e-mail addresses as usernames and an attacker attempts to initiate an [authentication](#authentication) [ceremony](#ceremony) for "alex.mueller@example.com" and the [Relying Party](#relying-party) responds with a failure, but then successfully initiates an [authentication ceremony](#authentication-ceremony) for "j.doe@example.com", then the attacker can conclude that "j.doe@example.com" is registered and "alex.mueller@example.com" is not. The [Relying Party](#relying-party) has thus leaked the possibly sensitive information that "j.doe@example.com" has a [user account](#user-account) at this [Relying Party](#relying-party).

The following is a non-normative, non-exhaustive list of measures the [Relying Party](#relying-party) may implement to mitigate or prevent information leakage due to such an attack:

- For [registration ceremonies](#registration-ceremony):
	- If the [Relying Party](#relying-party) uses [Relying Party](#relying-party) -specific usernames to identify users:
		- When initiating a [registration ceremony](#registration-ceremony), disallow registration of usernames that are syntactically valid e-mail addresses.
			Note: The motivation for this suggestion is that in this case the [Relying Party](#relying-party) probably has no choice but to fail the [registration ceremony](#registration-ceremony) if the user attempts to register a username that is already registered, and an information leak might therefore be unavoidable. By disallowing e-mail addresses as usernames, the impact of the leakage can be mitigated since it will be less likely that a user has the same username at this [Relying Party](#relying-party) as at other [Relying Parties](#relying-party).
		- If the [Relying Party](#relying-party) uses e-mail addresses to identify users:
		- When initiating a [registration ceremony](#registration-ceremony), interrupt the user interaction after the e-mail address is supplied and send a message to this address, containing an unpredictable one-time code and instructions for how to use it to proceed with the ceremony. Display the same message to the user in the web interface regardless of the contents of the sent e-mail and whether or not this e-mail address was already registered.
			Note: This suggestion can be similarly adapted for other externally meaningful identifiers, for example, national ID numbers or credit card numbers — if they provide similar out-of-band contact information, for example, conventional postal address.
- For [authentication ceremonies](#authentication-ceremony):
	- If, when initiating an [authentication ceremony](#authentication-ceremony), there is no [user account](#user-account) matching the provided username, continue the ceremony by invoking `navigator.credentials.get()` using a syntactically valid `PublicKeyCredentialRequestOptions` object that is populated with plausible imaginary values.
		This approach could also be used to mitigate information leakage via `allowCredentials`; see [§ 13.4.7 Unprotected account detection](#sctn-unprotected-account-detection) and [§ 14.6.3 Privacy leak via credential IDs](#sctn-credential-id-privacy-leak).
		Note: The username may be "provided" in various [Relying Party](#relying-party) -specific fashions: login form, session cookie, etc.
		Note: If returned imaginary values noticeably differ from actual ones, clever attackers may be able to discern them and thus be able to test for existence of actual accounts. Examples of noticeably different values include if the values are always the same for all username inputs, or are different in repeated attempts with the same username input. The `allowCredentials` member could therefore be populated with pseudo-random values derived deterministically from the username, for example.
		- When verifying an `AuthenticatorAssertionResponse` response from the [authenticator](#authenticator), make it indistinguishable whether verification failed because the signature is invalid or because no such user or credential is registered.
		- Perform a multi-step [authentication ceremony](#authentication-ceremony), e.g., beginning with supplying username and password or a session cookie, before initiating the WebAuthn [ceremony](#ceremony) as a subsequent step. This moves the username enumeration problem from the WebAuthn step to the preceding authentication step, where it may be easier to solve.

#### 14.6.3. Privacy leak via credential IDs

*This section is not normative.*

This privacy consideration applies to [Relying Parties](#relying-party) that support [authentication ceremonies](#authentication-ceremony) with a non- [empty](https://infra.spec.whatwg.org/#list-empty) `allowCredentials` argument as the first authentication step. For example, if using authentication with [server-side credentials](#server-side-credential) as the first authentication step.

In this case the `allowCredentials` argument risks leaking personally identifying information, since it exposes the user’s [credential IDs](#credential-id) to an unauthenticated caller. [Credential IDs](#credential-id) are designed to not be correlatable between [Relying Parties](#relying-party), but the length of a [credential ID](#credential-id) might be a hint as to what type of [authenticator](#authenticator) created it. It is likely that a user will use the same username and set of [authenticators](#authenticator) for several [Relying Parties](#relying-party), so the number of [credential IDs](#credential-id) in `allowCredentials` and their lengths might serve as a global correlation handle to de-anonymize the user. Knowing a user’s [credential IDs](#credential-id) also makes it possible to confirm guesses about the user’s identity given only momentary physical access to one of the user’s [authenticators](#authenticator).

In order to prevent such information leakage, the [Relying Party](#relying-party) could for example:

- Perform a separate authentication step, such as username and password authentication or session cookie authentication, before initiating the WebAuthn [authentication ceremony](#authentication-ceremony) and exposing the user’s [credential IDs](#credential-id).
- Use [client-side discoverable credentials](#client-side-discoverable-credential), so the `allowCredentials` argument is not needed.

If the above prevention measures are not available, i.e., if `allowCredentials` needs to be exposed given only a username, the [Relying Party](#relying-party) could mitigate the privacy leak using the same approach of returning imaginary [credential IDs](#credential-id) as discussed in [§ 14.6.2 Username Enumeration](#sctn-username-enumeration).

When [signalling](#signal-methods) that a [credential id](#credential-id) was not recognized, the [WebAuthn Relying Party](#webauthn-relying-party) SHOULD use the `signalUnknownCredential(options)` method instead of the `signalAllAcceptedCredentials(options)` method to avoid exposing [credential IDs](#credential-id) to an unauthenticated caller.

## 15\. Accessibility Considerations

[User verification](#user-verification) -capable [authenticators](#authenticator), whether [roaming](#roaming-authenticators) or [platform](#platform-authenticators), should offer users more than one user verification method. For example, both fingerprint sensing and PIN entry. This allows for fallback to other user verification means if the selected one is not working for some reason. Note that in the case of [roaming authenticators](#roaming-authenticators), the authenticator and platform might work together to provide a user verification method such as PIN entry [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)").

[Relying Parties](#relying-party), at [registration](#registration) time, SHOULD provide affordances for users to complete future [authorization gestures](#authorization-gesture) correctly. This could involve naming the authenticator, choosing a picture to associate with the device, or entering freeform text instructions (e.g., as a reminder-to-self).

### 15.1. Recommended Range for Ceremony Timeouts

[Ceremonies](#ceremony) relying on timing, e.g., a [registration ceremony](#registration-ceremony) (see `timeout`) or an [authentication ceremony](#authentication-ceremony) (see `timeout`), ought to follow [\[WCAG21\]](#biblio-wcag21 "Web Content Accessibility Guidelines (WCAG) 2.1") ’s [Guideline 2.2 Enough Time](https://www.w3.org/TR/WCAG21/#enough-time). If a [client platform](#client-platform) determines that a [Relying Party](#relying-party) -supplied timeout does not appropriately adhere to the latter [\[WCAG21\]](#biblio-wcag21 "Web Content Accessibility Guidelines (WCAG) 2.1") guidelines, then the [client platform](#client-platform) MAY adjust the timeout accordingly.

The is as follows:

- Recommended range: 300000 milliseconds to 600000 milliseconds.
- Recommended default value: 300000 milliseconds (5 minutes).

## 16\. Test Vectors

*This section is not normative.*

This section lists example values that may be used to validate implementations.

Examples are given as pseudocode in pairs of [registration](#registration-ceremony) and [authentication ceremonies](#authentication-ceremony) done with the same [credential](https://w3c.github.io/webappsec-credential-management/#concept-credential), with byte string literals and comments in CDDL [\[RFC8610\]](#biblio-rfc8610 "Concise Data Definition Language (CDDL): A Notational Convention to Express Concise Binary Object Representation (CBOR) and JSON Data Structures") notation. The examples are not exhaustive and do not include [WebAuthn extensions](#webauthn-extensions).

The examples are structured as a flow from inputs to outputs, including some intermediate values. In registration examples the [Relying Party](#relying-party) defines the `challenge` input, the [client](#client) generates the `clientDataJSON` output and the [authenticator](#authenticator) generates the `attestationObject` output. In authentication examples the [Relying Party](#relying-party) defines the `challenge` input, the [client](#client) generates the `clientDataJSON` output and the [authenticator](#authenticator) generates the `authenticatorData` and `signature` outputs. Other cryptographically unrelated inputs and outputs are not included.

[Authenticator](#authenticator) implementers may check that they produce similarly structured `attestationObject`, `authenticatorData` and `signature` outputs. [Client](#client) implementers may check that they produce similarly structured `clientDataJSON` outputs. [Relying Party](#relying-party) implementers may check that they can successfully validate the registration outputs given the same `challenge` input, and that they can successfully validate the authentication outputs given the same `challenge` input and the [credential public key](#credential-public-key) and [credential ID](#credential-id) from the associated registration example.

All examples use the [RP ID](#rp-id) `example.org`, the `origin` `https://example.org` and, where applicable, the `topOrigin` `https://example.com`. Examples include [no attestation](#none) when not noted otherwise.

All random values are deterministically generated using HKDF-SHA-256 [\[RFC5869\]](#biblio-rfc5869 "HMAC-based Extract-and-Expand Key Derivation Function (HKDF)") from the base input key material denoted in CDDL as `'WebAuthn test vectors'`, or equivalently as `h'576562417574686e207465737420766563746f7273'`. ECDSA signatures use a deterministic nonce [\[RFC6979\]](#biblio-rfc6979 "Deterministic Usage of the Digital Signature Algorithm (DSA) and Elliptic Curve Digital Signature Algorithm (ECDSA)"). The RSA key in the examples is constructed from the two smallest Mersenne primes 2 <sup>p</sup> - 1 such that p ≥ 1024.

Note that:

- Although the examples include [credential private keys](#credential-private-key) and [attestation private keys](#attestation-private-key) for reproducibility, these would normally not be shared with the [client](#client) or [Relying Party](#relying-party).
- Although each example uses a different [AAGUID](#aaguid), the [AAGUID](#aaguid) would normally be constant for a given [authenticator](#authenticator).

Note: [Authenticators](#authenticator) implementing CTAP2 [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") return [attestation objects](#attestation-object) using different keys than those defined in this specification. These examples reflect the attestation object format expected by [WebAuthn Relying Parties](#webauthn-relying-party), so [attestation objects](#attestation-object) emitted from CTAP2 need their keys translated in order to be bitwise identical to these examples.

### 16.1. Attestation trust root certificate

All examples that include [attestation](#attestation) use the attestation trust root certificate given as `attestation_ca_cert` below, encoded in X.509 DER [\[RFC5280\]](#biblio-rfc5280 "Internet X.509 Public Key Infrastructure Certificate and Certificate Revocation List (CRL) Profile"):

```
attestation_ca_key = h'7809337f05740a96a78eedf9e9280499dcc8f2aa129616049ec1dccfe103eb2a'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='Attestation CA', L=32)
attestation_ca_serial_number = h'ed7f905d8bd0b414d1784913170a90b6'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='Attestation CA', L=16)
attestation_ca_cert = h'30820207308201ada003020102021100ed7f905d8bd0b414d1784913170a90b6300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a3062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d030107034200043269300e5ff7b699015f70cf80a8763bf705bc2e2af0c1b39cff718b7c35880ca30f319078d91b03389a006fdfc8a1dcd84edfa07d30aa13474a248a0dab5baaa3423040300f0603551d130101ff040530030101ff300e0603551d0f0101ff040403020106301d0603551d0e0416041445aff715b0dd786741fee996ebc16547a3931b1e300a06082a8648ce3d04030203480030450220483063b6bb08dcc83da33a02c11d2f42203176893554d138c614a36908724cc8022100f5ef2c912d4500b3e2f5b591d0622491e9f220dfd1f9734ec484bb7e90887663'
```

### 16.2. ES256 Credential with No Attestation

[Registration](#registration-ceremony):

```
challenge = h'00c30fb78531c464d2b6771dab8d7b603c01162f2fa486bea70f283ae556e130'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='none.ES256', L=32)

credential_private_key = h'6e68e7a58484a3264f66b77f5d6dc5bc36a47085b615c9727ab334e8c369c2ee'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='none.ES256', L=32)
client_data_gen_flags = h'f9'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='none.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'06441e0e375c4c1ad70620302532c4e5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='none.ES256', L=16)
aaguid = h'8446ccb9ab1db374750b2367ff6f3a1f'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='none.ES256', L=16)
credential_id = h'f91f391db4c9b2fde0ea70189cba3fb63f579ba6122b33ad94ff3ec330084be4'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='none.ES256', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'ba'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='none.ES256', L=1)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a22414d4d507434557878475453746e63647134313759447742466938767049612d7077386f4f755657345441222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a20426b5165446a646354427258426941774a544c4535513d3d227d'
attestationObject = h'a363666d74646e6f6e656761747453746d74a068617574684461746158a4bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b559000000008446ccb9ab1db374750b2367ff6f3a1f0020f91f391db4c9b2fde0ea70189cba3fb63f579ba6122b33ad94ff3ec330084be4a5010203262001215820afefa16f97ca9b2d23eb86ccb64098d20db90856062eb249c33a9b672f26df61225820930a56b87a2fca66334b03458abf879717c12cc68ed73290af2e2664796b9220'
```

[Authentication](#authentication-ceremony):

```
challenge = h'39c0e7521417ba54d43e8dc95174f423dee9bf3cd804ff6d65c857c9abf4d408'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='none.ES256', L=32)

client_data_gen_flags = h'4a'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='none.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'38'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='none.ES256', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b51900000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a224f63446e55685158756c5455506f334a5558543049393770767a7a59425039745a63685879617630314167222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
signature = h'3046022100f50a4e2e4409249c4a853ba361282f09841df4dd4547a13a87780218deffcd380221008480ac0f0b93538174f575bf11a1dd5d78c6e486013f937295ea13653e331e87'
```

### 16.3. ES256 Credential with Self Attestation

[Registration](#registration-ceremony):

```
challenge = h'7869c2b772d4b58eba9378cf8f29e26cf935aa77df0da89fa99c0bdc0a76f7e5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='packed-self.ES256', L=32)

credential_private_key = h'b4bbfa5d68e1693b6ef5a19a0e60ef7ee2cbcac81f7fec7006ac3a21e0c5116a'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='packed-self.ES256', L=32)
client_data_gen_flags = h'db'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='packed-self.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'53d8535ef284d944643276ffd3160756'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='packed-self.ES256', L=16)
aaguid = h'df850e09db6afbdfab51697791506cfc'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='packed-self.ES256', L=16)
credential_id = h'455ef34e2043a87db3d4afeb39bbcb6cc32df9347c789a865ecdca129cbef58c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='packed-self.ES256', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'fd'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='packed-self.ES256', L=1)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a2265476e4374334c55745936366b336a506a796e6962506b31716e666644616966715a774c33417032392d55222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a205539685458764b453255526b4d6e625f3078594856673d3d227d'
attestationObject = h'a363666d74667061636b65646761747453746d74a263616c67266373696758483046022100ae045923ded832b844cae4d5fc864277c0dc114ad713e271af0f0d371bd3ac540221009077a088ed51a673951ad3ba2673d5029bab65b64f4ea67b234321f86fcfac5d68617574684461746158a4bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b55d00000000df850e09db6afbdfab51697791506cfc0020455ef34e2043a87db3d4afeb39bbcb6cc32df9347c789a865ecdca129cbef58ca5010203262001215820eb151c8176b225cc651559fecf07af450fd85802046656b34c18f6cf193843c5225820927b8aa427a2be1b8834d233a2d34f61f13bfd44119c325d5896e183fee484f2'
```

[Authentication](#authentication-ceremony):

```
challenge = h'4478a10b1352348dd160c1353b0d469b5db19eb91c27f7dfa6fed39fe26af20b'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='packed-self.ES256', L=32)

client_data_gen_flags = h'1f'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='packed-self.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'8136f9debcfa121496a265c6ce2982d5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='packed-self.ES256', L=16)
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'a1'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='packed-self.ES256', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50900000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a225248696843784e534e493352594d45314f7731476d3132786e726b634a5f6666707637546e2d4a71386773222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a206754623533727a36456853576f6d58477a696d4331513d3d227d'
signature = h'3044022076691be76a8618976d9803c4cdc9b97d34a7af37e3bdc894a2bf54f040ffae850220448033a015296ffb09a762efd0d719a55346941e17e91ebf64c60d439d0b9744'
```

### 16.4. ES256 Credential with "crossOrigin": true in clientDataJSON

[Registration](#registration-ceremony):

```
challenge = h'3be5aacd03537142472340ab5969f240f1d87716e20b6807ac230655fa4b3b49'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='none.ES256.crossOrigin', L=32)

credential_private_key = h'96c940e769bd9f1237c119f144fa61a4d56af0b3289685ae2bef7fb89620623d'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='none.ES256.crossOrigin', L=32)
client_data_gen_flags = h'71'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='none.ES256.crossOrigin', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'cd9aae12d0d1f435aaa56e6d0564c5ba'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='none.ES256.crossOrigin', L=16)
aaguid = h'883f4f6014f19c09d87aa38123be48d0'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='none.ES256.crossOrigin', L=16)
credential_id = h'6e1050c0d2ca2f07c755cb2c66a74c64fa43065c18f938354d9915db2bd5ce57'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='none.ES256.crossOrigin', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'27'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='none.ES256.crossOrigin', L=1)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a224f2d57717a514e5463554a484930437257576e7951504859647862694332674872434d475666704c4f306b222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a747275652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a207a5a71754574445239445771705735744257544675673d3d227d'
attestationObject = h'a363666d74646e6f6e656761747453746d74a068617574684461746158a4bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b54500000000883f4f6014f19c09d87aa38123be48d000206e1050c0d2ca2f07c755cb2c66a74c64fa43065c18f938354d9915db2bd5ce57a501020326200121582022200a473f90b11078851550d03b4e44a2279f8c4eca27b3153dedfe03e4e97d225820cbd0be95e746ad6f5a8191be11756e4c0420e72f65b466d39bc56b8b123a9c6e'
```

[Authentication](#authentication-ceremony):

```
challenge = h'876aa517ba83fdee65fcffdbca4c84eeae5d54f8041a1fc85c991e5bbb273137'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='none.ES256.crossOrigin', L=32)

client_data_gen_flags = h'57'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='none.ES256.crossOrigin', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'f76a5c4d50f401bcbeab876d9a3e9e7e'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='none.ES256.crossOrigin', L=16)
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'0c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='none.ES256.crossOrigin', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50500000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a226832716c463771445f65356c5f505f62796b7945377135645650674547685f49584a6b655737736e4d5463222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a747275652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a2039327063545644304162792d713464746d6a366566673d3d227d'
signature = h'304402204396b14b216ed47920dc359e46aa0a1d4a912cf9d50f25a58ec236a11db4cf5e02204fdb59ff01656c4b0868e415436a464b0e30e94b02c719b995afaba9c917146b'
```

### 16.5. ES256 Credential with "topOrigin" in clientDataJSON

[Registration](#registration-ceremony):

```
challenge = h'4e1f4c6198699e33c14f192153f49d7e0e8e3577d5ac416c5f3adc92a41f27e5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='none.ES256.topOrigin', L=32)

credential_private_key = h'a2d6de40ab974b80d8c1ef78c6d4300097754f7e016afe7f8ea0ad9798b0d420'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='none.ES256.topOrigin', L=32)
client_data_gen_flags = h'54'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='none.ES256.topOrigin', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
aaguid = h'97586fd09799a76401c200455099ef2a'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='none.ES256.topOrigin', L=16)
credential_id = h'b8ad59b996047ab18e2ceb57206c362da57458793481f4a8ebf101c7ca7cc0f1'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='none.ES256.topOrigin', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'a0'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='none.ES256.topOrigin', L=1)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a225468394d595a68706e6a504254786b68555f53646667364f4e58665672454673587a72636b7151664a2d55222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a747275652c22746f704f726967696e223a2268747470733a2f2f6578616d706c652e636f6d227d'
attestationObject = h'a363666d74646e6f6e656761747453746d74a068617574684461746158a4bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b5410000000097586fd09799a76401c200455099ef2a0020b8ad59b996047ab18e2ceb57206c362da57458793481f4a8ebf101c7ca7cc0f1a5010203262001215820a1c47c1d82da4ebe82cd72207102b380670701993bc35398ae2e5726427fe01d22582086c1080d82987028c7f54ecb1b01185de243b359294a0ed210cd47480f0adc88'
```

[Authentication](#authentication-ceremony):

```
challenge = h'd54a5c8ca4b62a8e3bb321e3b2bc73856f85a10150db2939ac195739eb1ea066'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='none.ES256.topOrigin', L=32)

client_data_gen_flags = h'77'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='none.ES256.topOrigin', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'52216824c5514070c0156162e2fc54a5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='none.ES256.topOrigin', L=16)
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'9f'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='none.ES256.topOrigin', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50500000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a22315570636a4b53324b6f34377379486a7372787a68572d466f51465132796b3572426c584f6573656f4759222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a747275652c22746f704f726967696e223a2268747470733a2f2f6578616d706c652e636f6d222c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a205569466f4a4d565251484441465746693476785570513d3d227d'
signature = h'304402206a19613fa8cfacfc8027272aec5dae3555fea9f983d841581466678d71e6761a02207a9785ba22e48eb18525850357d9dc70795aaad2e6021159c4a4a183146eaa71'
```

### 16.6. ES256 Credential with very long credential ID

[Registration](#registration-ceremony):

```
challenge = h'1113c7265ccf5e65124282fa1d7819a7a14cb8539aa4cdbec7487e5f35d8ec6c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='none.ES256.long-credential-id', L=32)

credential_private_key = h'6fd2149bb5f1597fe549b138794bde61893b2dc32ca316de65f04808dac211dc'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='none.ES256.long-credential-id', L=32)
client_data_gen_flags = h'90'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='none.ES256.long-credential-id', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
aaguid = h'8f3360c2cd1b0ac14ffe0795c5d2638e'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='none.ES256.long-credential-id', L=16)
credential_id = h'3a761a4e1674ad6c4305869435c0eee9c286172c229bb91b48b4ada140c0863417031305cce5b4a27a88d7fe728a5f5a627de771b4b40e77f187980c124f9fe832d7136010436a056cce716680587d23187cf1fc2c62ae86fc3e508ee9617ffc74fbc10488ec16ec5e9096328669a898709b655e549738c666c1ae6281dc3b5f733c251d3eefb76ee70a3805ca91bcc18e49c8dc7f63ebcb486ba8c3d6ab52b88ff72c6a5bb47c32f3ee8683a3ddc8abf60870448ec8a21b5bdcb183c7dead870255575a6df96eb1b6a2a1019780cba9e4887b17ff1164bbbcc10eb0d86ed75984cd3fa3419103024507dfd9ce8f92c56af7914cb0bb50b87ba82a312bb7dcd93028dbdcd6adb266979667158335171e3682d37755701edbf9d872846a291d49e57ef09da1ec637f5052ed2aa7407f7e61827468e94b461844f4c67be5fa9c6055a566f8fdfc29d4bf78a9ff275f552cc68ba543fa3962eea36fd1ea8453764577d021d0a181efc1f6100ab2e4110039e21ee16970bda7432b6134492155afc126295b3a2eccd12c66a68e340969e995e3e8c9c476e395cfc21203414110779474f1c9797406637dbe414f132519d3bf0ce4f01734ef0e1a12c3ad604ff15d766b1624db6a5a7ccbff7bc35c9908df94aba277e0af48f04ff3d16381c47e5a37ed3988a67a3b1ecaa926336b33391fff04128f869991c9fabd905b6fe3ceef5f8b630ec1c5d2636d5b1961ad5ca5004170f6f5e482792aad989b0287fe91e5c479403397152f1fa56aa79b156eb47e6c8ea3eb175c34cfb38ad8e772874639b1023d4d01395c94e55831671cc022aa6fa1e02a02c2e4abc776f6960e51f83b71a8c0f207b6a347573977812c9aa5480b0011aa739bd4b76c18c000cc4757cceccb920f007c40c00e37e5ab21476cd9f6054a8fffb55a108f5c706e2cea2049d81fd321ff47d2a5761b0800955ab1d4f4889f55a84e2601c684f17a4ade7453ea49591d0b59c8d9a765052f62219cf6ef4a5dd9539f0617d6ebbebce7c000455475d18449e25c49ef9a1e3efe18c09082ebe2058d7c347defaa92f0664553b805c7d76bbfce5f330aca220ac90a789380fc479ea0d8793205813cca590a912f699ad52f991a1bc0a503c3ec4b2a696719e3c26591a87127f7305cc7e72f4c8e39355ebb06a5b1042990f38710ee7aa612ee4374bb82e878585a70a96c2a6b47f101a4ff154be4fd76a3167577a5cc54d9167c154c69ac35485e44cc898b719e1be3cc9c0fb5624b8f8a0dae10947a41bf848b6c1bb33d1006ec077d7e286e3f2a7b4843716390119449fe2721e81a5ed2333d331c7120765da58fadae73c19d9a8c4509cf8ac1e9d98b799a5274509069739b5823f3fb496663820033426988eefca53e580e0f9e0dfe0992fc2e53a97e053639f98577058f995bdbd41cefdb'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='none.ES256.long-credential-id', L=1023)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'69'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='none.ES256.long-credential-id', L=1)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a22455250484a6c7a50586d5553516f4c364858675a7036464d75464f61704d322d7830682d587a5859374777222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
attestationObject = h'a363666d74646e6f6e656761747453746d74a0686175746844617461590483bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b549000000008f3360c2cd1b0ac14ffe0795c5d2638e03ff3a761a4e1674ad6c4305869435c0eee9c286172c229bb91b48b4ada140c0863417031305cce5b4a27a88d7fe728a5f5a627de771b4b40e77f187980c124f9fe832d7136010436a056cce716680587d23187cf1fc2c62ae86fc3e508ee9617ffc74fbc10488ec16ec5e9096328669a898709b655e549738c666c1ae6281dc3b5f733c251d3eefb76ee70a3805ca91bcc18e49c8dc7f63ebcb486ba8c3d6ab52b88ff72c6a5bb47c32f3ee8683a3ddc8abf60870448ec8a21b5bdcb183c7dead870255575a6df96eb1b6a2a1019780cba9e4887b17ff1164bbbcc10eb0d86ed75984cd3fa3419103024507dfd9ce8f92c56af7914cb0bb50b87ba82a312bb7dcd93028dbdcd6adb266979667158335171e3682d37755701edbf9d872846a291d49e57ef09da1ec637f5052ed2aa7407f7e61827468e94b461844f4c67be5fa9c6055a566f8fdfc29d4bf78a9ff275f552cc68ba543fa3962eea36fd1ea8453764577d021d0a181efc1f6100ab2e4110039e21ee16970bda7432b6134492155afc126295b3a2eccd12c66a68e340969e995e3e8c9c476e395cfc21203414110779474f1c9797406637dbe414f132519d3bf0ce4f01734ef0e1a12c3ad604ff15d766b1624db6a5a7ccbff7bc35c9908df94aba277e0af48f04ff3d16381c47e5a37ed3988a67a3b1ecaa926336b33391fff04128f869991c9fabd905b6fe3ceef5f8b630ec1c5d2636d5b1961ad5ca5004170f6f5e482792aad989b0287fe91e5c479403397152f1fa56aa79b156eb47e6c8ea3eb175c34cfb38ad8e772874639b1023d4d01395c94e55831671cc022aa6fa1e02a02c2e4abc776f6960e51f83b71a8c0f207b6a347573977812c9aa5480b0011aa739bd4b76c18c000cc4757cceccb920f007c40c00e37e5ab21476cd9f6054a8fffb55a108f5c706e2cea2049d81fd321ff47d2a5761b0800955ab1d4f4889f55a84e2601c684f17a4ade7453ea49591d0b59c8d9a765052f62219cf6ef4a5dd9539f0617d6ebbebce7c000455475d18449e25c49ef9a1e3efe18c09082ebe2058d7c347defaa92f0664553b805c7d76bbfce5f330aca220ac90a789380fc479ea0d8793205813cca590a912f699ad52f991a1bc0a503c3ec4b2a696719e3c26591a87127f7305cc7e72f4c8e39355ebb06a5b1042990f38710ee7aa612ee4374bb82e878585a70a96c2a6b47f101a4ff154be4fd76a3167577a5cc54d9167c154c69ac35485e44cc898b719e1be3cc9c0fb5624b8f8a0dae10947a41bf848b6c1bb33d1006ec077d7e286e3f2a7b4843716390119449fe2721e81a5ed2333d331c7120765da58fadae73c19d9a8c4509cf8ac1e9d98b799a5274509069739b5823f3fb496663820033426988eefca53e580e0f9e0dfe0992fc2e53a97e053639f98577058f995bdbd41cefdba50102032620012158203b8176b7504489cc593046d7988abb7905a742de6ac2cdc748a873c663e90cb12258201436d5edc9a75f23999eef9d5950a5c2455514ee1014084720f841a06b828a11'
```

[Authentication](#authentication-ceremony):

```
challenge = h'ef1deba56dce48f674a447ccf63b9599258ce87648e5c396f2ef0ca1da460e3b'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='none.ES256.long-credential-id', L=32)

client_data_gen_flags = h'80'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='none.ES256.long-credential-id', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'e5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='none.ES256.long-credential-id', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50d00000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a22377833727057334f53505a307045664d396a75566d53574d36485a4935634f573875384d6f647047446a73222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
signature = h'304502203ecef83fb12a0cae7841055f9f87103a99fd14b424194bbf06c4623d3ee6e3fd022100d2ace346db262b1374a6b70faa51f518a42ddca13a4125ce6f5052a75bac9fb6'
```

### 16.7. Packed Attestation with ES256 Credential

[Registration](#registration-ceremony):

```
challenge = h'c1184a5fddf8045e13dc47f54b61f5a656b666b59018f16d870e9256e9952012'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='packed.ES256', L=32)

credential_private_key = h'36ed7bea2357cefa8c4ec7e134f3312d2e6ca3058519d0bcb4c1424272010432'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='packed.ES256', L=32)
client_data_gen_flags = h'8d'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='packed.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'f5af1b3588ca0a05ab05753e7c29756a'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='packed.ES256', L=16)
aaguid = h'876ca4f52071c3e9b25509ef2cdf7ed6'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='packed.ES256', L=16)
credential_id = h'c9a6f5b3462d02873fea0c56862234f99f081728084e511bb7760201a89054a5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='packed.ES256', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'4f'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='packed.ES256', L=1)
attestation_private_key = h'ec2804b222552b4b277d1f58f8c4343c0b0b0db5474eb55365c89d66a2bc96be'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='packed.ES256', L=32)
attestation_cert_serial_number = h'88c220f83c8ef1feafe94deae45faad0'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='packed.ES256', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a227752684b58393334424634543345663153324831706c61325a725751475046746877365356756d56494249222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a20396138624e596a4b436757724258552d66436c3161673d3d227d'
attestationObject = h'a363666d74667061636b65646761747453746d74a363616c67266373696758473045022025fcee945801b94e63d7c029e6f761654cf02e7100d5364a3b90e03daa6276fc022100eabcdf4ce19feb0980e829c3b6137079b18e42f43ce5c3c573b83368794f354c637835638159022530820221308201c8a00302010202110088c220f83c8ef1feafe94deae45faad0300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a305f311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d03010703420004a91ba4389409dd38a428141940ca8feb1ac0d7b4350558104a3777a49322f3798440f378b3398ab2d3bb7bf91322c92eb23556f59ad0a836fec4c7663b0e4dc3a360305e300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e04160414a589ba72d060842ab11f74fb246bdedab16f9b9b301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e300a06082a8648ce3d040302034700304402201726b9d85ecd8a5ed51163722ca3a20886fd9b242a0aa0453d442116075defd502207ef471e530ac87961a88a7f0d0c17b091ffc6b9238d30f79f635b417be5910e768617574684461746158a4bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b54d00000000876ca4f52071c3e9b25509ef2cdf7ed60020c9a6f5b3462d02873fea0c56862234f99f081728084e511bb7760201a89054a5a50102032620012158201cf27f25da591208a4239c2e324f104f585525479a29edeedd830f48e77aeae522582059e4b7da6c0106e206ce390c93ab98a15a5ec3887e57f0cc2bece803b920c423'
```

[Authentication](#authentication-ceremony):

```
challenge = h'b1106fa46a57bef1781511c0557dc898a03413d5f0f17d244630c194c7e1adb5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='packed.ES256', L=32)

client_data_gen_flags = h'75'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='packed.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'019330c8cc486c3f3eba0b85369eabf1'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0b', info='packed.ES256', L=16)
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'46'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0c', info='packed.ES256', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50d00000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a2273524276704770587676463446524841565833496d4b4130453958773858306b526a44426c4d6668726255222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a20415a4d77794d78496244382d756775464e70367238513d3d227d'
signature = h'30460221009d8d54895393894d37b9fa7bdfbcff05403de3cf0d6443ffb394fa239f101579022100c8871288f19c6c48a3b64c09d39868c12d16ed80ea4c5d8890288975c0272f50'
```

### 16.8. Packed Attestation with ES384 Credential

[Registration](#registration-ceremony):

```
challenge = h'567b030b3e186bc1d169dd45b79f9e0d86f1fd63474da3eade5bdb8db379a0c3'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='packed.ES384', L=32)

credential_private_key = h'271e37d309c558c0f35222b37abba7500377d68e179e4c74b0cb558551b2e5276b47b90a317ca8ebbe1a12c93c2d5dd9'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='packed.ES384', L=48)
client_data_gen_flags = h'32'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='packed.ES384', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
aaguid = h'e950dcda3bdae1d087cda380a897848b'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='packed.ES384', L=16)
credential_id = h'953ae2dd9f28b1a1d5802c83e1f65833bb9769a08de82d812bc27c13fc6f06a9'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='packed.ES384', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'db'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='packed.ES384', L=1)
attestation_private_key = h'8d979fbb6e49c4eeb5925a2bca0fcdb023d3fb90bcadce8391da9da4ed2aee9a'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='packed.ES384', L=32)
attestation_cert_serial_number = h'3d0a5588bb87ebb1d4cee4a1807c1b7c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='packed.ES384', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a22566e7344437a3459613848526164314674352d65445962785f574e4854615071336c76626a624e356f4d4d222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
attestationObject = h'a363666d74667061636b65646761747453746d74a363616c67266373696758473045022100c56ecc970b7843833e0f461fde26233f61eb395161d481558c08b9c6ed61675b022029f5e05033705cd0f9b0a07e149468ec308a4f84906409efdceb1da20a7518d6637835638159022530820221308201c7a00302010202103d0a5588bb87ebb1d4cee4a1807c1b7c300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a305f311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d0301070342000417e5cc91d676d370e36aa7de40c25aacb45a3845f13d2932088ece2270b9b431241c219c22d0c256c9438ade00f2c05e62f8ef906b9b997ae9f3c460c2db66f5a360305e300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e04160414c7c8dd95382a2230e4c0dd3664338fa908169a9c301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e300a06082a8648ce3d0403020348003045022054068cc9ae038937b7c468c307edb9c6927ffdeb6a20070c483eb40330f99f10022100cf41953919c3c04693d6b1f42a613753f204e70e85fc6e9b17036170b83596e068617574684461746158c5bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b55900000000e950dcda3bdae1d087cda380a897848b0020953ae2dd9f28b1a1d5802c83e1f65833bb9769a08de82d812bc27c13fc6f06a9a5010203382220022158304866bd8b01da789e9eb806e5eab05ae5a638542296ab057a2f1bbce9b58f8a08b9171390b58a37ac7fffc2c5f45857da2258302a0b024c7f4b72072a1f96bd30a7261aae9571dd39870eb29e55c0941c6b08e89629a1ea1216aa64ce57c2807bf3901a'
```

[Authentication](#authentication-ceremony):

```
challenge = h'ff41c3d25dbd8966fb61e28ef5e47041e137ed268520412d76202ba0ad2d1453'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='packed.ES384', L=32)

client_data_gen_flags = h'0c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='packed.ES384', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'af'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='packed.ES384', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50d00000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a225f304844306c32396957623759654b4f39655277516545333753614649454574646941726f4b307446464d222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
signature = h'3065023100e4efbb46745ed00e67c4d51ab2bacab2af62ffa8b7c5fecec6d7d9bf2582275034a713a3dd731685eee81adfaf6aa63f0230161655353f07e018a3c2539f8de7c8c4cf88d4c32d2be29fe4e76fa096ecc9458bbfe0895d57129ab324130e6f0692db'
```

### 16.9. Packed Attestation with ES512 Credential

[Registration](#registration-ceremony):

```
challenge = h'4ee220cd92b07e11451cb4c201c5755bd879848e492a9b12d79135c62764dc2fd28ead4808cafe5ad1de8fa9e08d4a8eeafea4dfb333877b02bc503f475d3b0c1394a7683baaf4f2477829f7b8cf750948985558748c073068396fcfdcd3f245bf2038e6bb38d7532768aad13be8c118f727722e7426139041e9caca503884c5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='packed.ES512', L=128)

credential_private_key = h'f11120594f6a4944ac3ba59adbbc5b85016895b649f4cc949a610f4b48be47b318850bacb105f747647bba8852b6b8e52a0b3679f1bbbdfe18c99409bcb644fa45'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='packed.ES512', L=65)
client_data_gen_flags = h'6d'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='packed.ES512', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'a37a958ce2f6b535a6e06c64cc8fd082'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='packed.ES512', L=16)
aaguid = h'39d8ce6a3cf61025775083a738e5c254'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='packed.ES512', L=16)
credential_id = h'd17d5af7e3f37c56622a67c8462c9e1c6336dfccb8b61d359dc47378dba58ce4'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='packed.ES512', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'cf'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='packed.ES512', L=1)
attestation_private_key = h'ffbc89d5f75994f52dc5e7538ee269402d26995d40c16fb713473e34fca98be4'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='packed.ES512', L=32)
attestation_cert_serial_number = h'8a128b7ebe52b993835779e6d9b81355'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='packed.ES512', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a22547549677a5a4b7766684646484c544341635631573968356849354a4b707353313545317869646b33435f536a713149434d722d577448656a366e676a55714f3676366b33374d7a683373437646415f5231303744424f5570326737717654795233677039376a5064516c496d465659644977484d47673562385f63305f4a46767941343572733431314d6e614b72524f2d6a424750636e636935304a684f5151656e4b796c4134684d55222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a206f3371566a4f4c327454576d3447786b7a495f5167673d3d227d'
attestationObject = h'a363666d74667061636b65646761747453746d74a363616c67266373696758483046022100c48fcbd826bbc79680802026688d41ab6da8c3a1d22ab6cecf36c8d7695d22500221008767dfe591277e973078d5692c8c35cf9d579792822e7145c96a0ac4515df5b0637835638159022730820223308201c8a0030201020211008a128b7ebe52b993835779e6d9b81355300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a305f311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d03010703420004940b68885291536e2f7c60c05acfb252e7eebcf4304425dd93ab7b1962f20492bf18dc0f12862599e81fb764ac92151f9a78fcbb35d7a26c8c52949b18133c06a360305e300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e041604143ffad863abcd3dc5717b8a252189f41af97e7f31301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e300a06082a8648ce3d0403020349003046022100832c8b64c4f0188bd32e1bec63e13301cdc03165d3ef840d1f3dabb9a5719f83022100add57a9d5bedec98f29222dfc97ea795d055ee13a02a153d02be9ce00aedeb9168617574684461746158e9bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b54d0000000039d8ce6a3cf61025775083a738e5c2540020d17d5af7e3f37c56622a67c8462c9e1c6336dfccb8b61d359dc47378dba58ce4a5010203382320032158420083240a2c3ad21a3dc0a6daa3d8bc05a46d7cd9825ba010ae2a22686c2d6d663d7d5f678987fb1e767542e63dc197ae915e25f8ee284651af29066910a2cc083f50225842017337df47ab5cce5d716ef8caffa97a3012689b1f326ea6c43a1ba9596c72f71f0122390143552b42be772b4c35ffb961220c743b486a601ea4cb6d5412f5b078d3'
```

[Authentication](#authentication-ceremony):

```
challenge = h'08d3190c6dcb3d4f0cb659a0333bf5ea124ddf36a0cd33d5204b0d7a22a8cc26f2e4f169d200285c77b3fb22e0f1c7f49a87d4be2d25e92d797808ddaaa9b5715efd3a6ada9339d3052a687dbc5d2f8c871b0451e0691f57ad138541b7b72e7aa8933729ec1c664bf2e4dedae1616d08ecefa80a2a53b103663ce5a881048829'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='packed.ES512', L=128)

client_data_gen_flags = h'ac'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='packed.ES512', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'52'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0b', info='packed.ES512', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b51900000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a22434e4d5a4447334c5055384d746c6d674d7a763136684a4e337a61677a5450564945734e65694b6f7a436279355046703067416f5848657a2d794c67386366306d6f66557669306c3653313565416a6471716d31635637394f6d72616b7a6e544253706f666278644c3479484777525234476b66563630546855473374793536714a4d334b6577635a6b7679354e376134574674434f7a7671416f71553745445a6a7a6c7149454569436b222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
signature = h'3081870242009bda02fe384e77bcb9fb42b07c395b7a53ec9d9616dd0308ab8495c2141c8364c7d16e212a4a4fb8e3987ff6c99eafd64d8484fd28c3fc7968f658a9033d1bb1b802416383e9f3ee20c691b66620299fef36bea2df4d39c92b2ead92f58e7b79ab0d9864d2ebf3b0dcc66ea13234492ccee6e9d421db43c959bcb94c162dc9494136c9f6'
```

### 16.10. Packed Attestation with RS256 Credential

[Registration](#registration-ceremony):

```
challenge = h'bea8f0770009bd57f2c0df6fea9f743a27e4b61bbe923c862c7aad7a9fc8e4a6'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='packed.RS256', L=32)

; The two smallest Mersenne primes 2^p - 1 where p >= 1024
private_key_p = 2^1279 - 1 = h'7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff'
private_key_q = 2^2203 - 1 = h'07ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff'
client_data_gen_flags = h'1c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='packed.RS256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
aaguid = h'428f8878298b9862a36ad8c7527bfef2'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='packed.RS256', L=16)
credential_id = h'992a18acc83f67533600c1138a4b4c4bd236de13629cf025ed17cb00b00b74df'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='packed.RS256', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'7e'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='packed.RS256', L=1)
attestation_private_key = h'08a1322d5aa5b5b40cd67c2cc30b038e7921d7888c84c342d50d79f0c5fc3464'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='packed.RS256', L=32)
attestation_cert_serial_number = h'1f6fb7a5ece81b45896b983a995da5f3'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='packed.RS256', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a2276716a776477414a76566679774e3976367039304f69666b7468752d6b6a79474c48717465705f49354b59222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
attestationObject = h'a363666d74667061636b65646761747453746d74a363616c672663736967584730450221008b8c5c6ea8c142c032e0be69e1353d44461c5c9109941cdda951b976eb95b6b302204d52f406c19e254b3ff9589bd18070fb055ac8db12fdd0a6734bea9d7168e900637835638159022630820222308201c7a00302010202101f6fb7a5ece81b45896b983a995da5f3300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a305f311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d03010703420004b7b36b7542a11120b443c794d0c99fdc25a06b76586413d81e086163ef6fe147a557afc34e2861d9057d6d465d4705a0310550bdeeb5f35ee35b9425ab859981a360305e300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e04160414fb37b647bccfb9e54d989eaaacc1633868703fb3301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e300a06082a8648ce3d0403020349003046022100b86bc129d92afca7d9869a39f70f139a305b4073a39eb654d81424bed5757d91022100cf9f7c60cab7c4a7d3e7f0020f281a93d4fd0a9f95121b989f56932a68885fba68617574684461746159021bbfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b55d00000000428f8878298b9862a36ad8c7527bfef20020992a18acc83f67533600c1138a4b4c4bd236de13629cf025ed17cb00b00b74dfa4010303390100205901b403fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff800000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000012143010001'
```

[Authentication](#authentication-ceremony):

```
challenge = h'295f59f5fa8fe62c5aca9e27626c78c8da376ae6d8cd2dd29aebad601e1bc4c5'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='packed.RS256', L=32)

client_data_gen_flags = h'0e'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='packed.RS256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'ba'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='packed.RS256', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b51900000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a224b56395a39667150356978617970346e596d7834794e6f33617562597a5333536d75757459423462784d55222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
signature = h'01063d52d7c39b4d432fc7063c5d93e582bdcb16889cd71f888d67d880ea730a428498d3bc8e1ee11f2b1ecbe6c292b118c55ffaaddefa8cad0a54dd137c51f1eec673f1bb6c4d1789d6826a222b22d0f585fc901fdc933212e579d199b89d672aa44891333e6a1355536025e82b25590256c3538229b55737083b2f6b9377e49e2472f11952f79fdd0da180b5ffd901b4049a8f081bb40711bef76c62aed943571f2d0575304cb549d68d8892f95086a30f93716aee818f8dc06e96c0d5e0ed4cfa9fd8773d90464b68cf140f7986666ff9c9e3302acd0535d60d769f465e2ab57ef8aabc89fccfef7ba32a64154a8b3d26be2298f470b8cc5377dbe3dfd4b0b45f8f01e63bde6cfc76b62771f9b70aa27cf40152cad93aa5acd784fd4b90f676e2ea828d0bf2400aebbaae4153e5838f537f88b6228346782a93a899be66ec77de45b3efcf311da6321c92e6b0cd11bfe653bf3e98cee8e341f02d67dbb6f9c98d9e8178090cfb5b70fbc6d541599ac794ae2f1d4de1286ec8de8c2daf7b1d15c8438e90d924df5c19045220a4c8438c1b979bbe016cf3d0eeec23c3999d4882cc645b776de930756612cdc6dd398160ff02a6'
```

### 16.11. Packed Attestation with Ed25519 Credential

[Registration](#registration-ceremony):

```
challenge = h'a8abf9dabdc6b0df63466b39bda9e8a34a34e185337a59f1c579990676d3b3bd'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='packed.EdDSA', L=32)

private_key = h'971f38c0f73aaf0c5a614eb5e26430ae1ea0ed13e4f425d96e9662349340b0b3'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='packed.EdDSA', L=32)
client_data_gen_flags = h'bd'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='packed.EdDSA', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'07f0d3e60ed90fffbd3932d85f922f11'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='packed.EdDSA', L=16)
aaguid = h'd5aa33581e8ca478e20fe713f5d32ff2'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='packed.EdDSA', L=16)
credential_id = h'ce9f840ed96599580cd140fbc7bb3230633f50f61041aff73308ae71caa8a2bd'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='packed.EdDSA', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'32'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='packed.EdDSA', L=1)
attestation_private_key = h'fbe7f950684f23afd045072a8b287ad29528707c662672850ac69733ffe0db85'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='packed.EdDSA', L=32)
attestation_cert_serial_number = h'b2cfc9ea33c8643b0e1a760463eaf164'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='packed.EdDSA', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a22714b763532723347734e396a526d733576616e6f6f306f303459557a656c6e7878586d5a426e6254733730222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a20425f44543567375a445f2d394f544c595835497645513d3d227d'
attestationObject = h'a363666d74667061636b65646761747453746d74a363616c67266373696758473045022100adecf9cace851c8bf4adb6b9e9dff8ddfa43092bbe04b5814cdf1c744970a88f02201c1bd55aacdfe2e4442c886132148b80394567018a382ce1fa260adae41e0746637835638159022730820223308201c8a003020102021100b2cfc9ea33c8643b0e1a760463eaf164300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a305f311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d03010703420004dd2b7a564b73b8c0b81c4c62e521925c4d1198ec9f583dbf1eebe364b65cd9c29a9bdf346aaa81fb6b9507e5249a52fdaf8e39e26b0b7dc45992a7e233b70f70a360305e300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e041604140ae27546bc7eccb1b4b597bd354f0c0b1f1f8f8e301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e300a06082a8648ce3d0403020349003046022100a0d434ecb5fc3bfd7da5f41904517ad2836249f561bd834ba7a438a8ab7a4ce8022100fac845bb7a02513b58e9f319654dbe49b0f02b95835bac568c71f8a18cdde9ab6861757468446174615881bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b54100000000d5aa33581e8ca478e20fe713f5d32ff20020ce9f840ed96599580cd140fbc7bb3230633f50f61041aff73308ae71caa8a2bda401010327200621582044e06ddd331c36a8dc667bab52bcae63486c916aa5e339e6acebaa84934bf832'
```

[Authentication](#authentication-ceremony):

```
challenge = h'895957e01c633a698348a2d8a31a54b7db27e8c1c43b2080d79ae2190267bfd2'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='packed.EdDSA', L=32)

client_data_gen_flags = h'8c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='packed.EdDSA', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'ab'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0b', info='packed.EdDSA', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50100000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a2269566c583442786a4f6d6d44534b4c596f7870557439736e364d48454f7943413135726947514a6e763949222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
signature = h'f5c59c7e46c34f6f8cc197101ddf9934fa2595f68eb1913a637e8419eb9ba4cfdfc48f85393bc0d40b011f0d6fecb097d6607525713223a0dc0d453993dae00b'
```

### 16.12. Packed Attestation with Ed448 Credential

[Registration](#registration-ceremony):

```
challenge = h'2578d0801b5a005b5451e540121788cb01949e187b91db13f58755403efbf337'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='packed.Ed448', L=32)

private_key = h'ed479eecf63bd89e3898434798bb3c417bfc8284f6f011958bc0e78edbf6a2a640c0e358b1b1452a1f3782c400dabb4134192dee3031869a45'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='packed.Ed448', L=57)
client_data_gen_flags = h'e3'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='packed.Ed448', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'050a80de27875521cc4c3316c06da42b'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='packed.Ed448', L=16)
aaguid = h'41c913aeda925fe02273322e34c2ae67'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='packed.Ed448', L=16)
credential_id = h'224fcde324e6b075ede55098a24b9ddce5f5a7c71d23703efd528a38f8a5f33c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='packed.Ed448', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'3b'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='packed.Ed448', L=1)
attestation_private_key = h'd90faf5cc3f7853456b09124dd870250347f9c9ff66dba363aecd9194c665715'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='packed.Ed448', L=32)
attestation_cert_serial_number = h'cff4228697d6e5ac47480b2390677f05'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='packed.Ed448', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a224a586a5167427461414674555565564145686549797747556e6868376b6473543959645651443737387a63222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a2042517141336965485653484d54444d577747326b4b773d3d227d'
attestationObject = h'a363666d74667061636b65646761747453746d74a363616c672663736967584730450220315c861030a51b01a3294e11acfeb83ffc2155971e9fb4ab566a25ce6a9e22c50221009fdd06e22c8628071913d176c9e52bf5ff4ab253a76a1aef0c831db4dc8791a1637835638159022730820223308201c8a003020102021100cff4228697d6e5ac47480b2390677f05300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a305f311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d03010703420004b85aaf790c824037cfe9fc56ab8d7ce6fbfaff2e3fe7c8d745734c3c6e3c6ce880d505ccdb1e2c3738680e6f49f475e4d8d0b6c29060e6e0d7a6392fb69094cea360305e300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e04160414fa8f81c2dcc0e194ae5034c7e79dcf6d9d8593e2301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e300a06082a8648ce3d0403020349003046022100e761f54215ad92f27c2c14b9eea3e39e8c22429e833ecba5be918987aa72e0e6022100d5a714df479c238586b7d9e6684ea84991087038b0fef6a29b57b66b74df05fd686175746844617461589bbfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b5590000000041c913aeda925fe02273322e34c2ae670020224fcde324e6b075ede55098a24b9ddce5f5a7c71d23703efd528a38f8a5f33ca4010103383420072158398051ef4f94670b5abf17da2e9558ba6eba94eb8704363915b4d666de287ad329de9f1f075211aba602dc6e7a5e52b15a8ee1c984a9f8887380'
```

[Authentication](#authentication-ceremony):

```
challenge = h'1a942f401d8d8e36fe888c35c22b718217802fc6685bf139c47b311408128693'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='packed.Ed448', L=32)

client_data_gen_flags = h'2d'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='packed.Ed448', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'5ca1e381b5e009e01760db2eb632316f'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0b', info='packed.Ed448', L=16)
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'fc'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0c', info='packed.Ed448', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b51d00000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a22477051765142324e6a6a622d6949773177697478676865414c385a6f575f4535784873784641675368704d222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a20584b486a6762586743654158594e7375746a497862773d3d227d'
signature = h'971c13fc11f64857ee2b2754b36430397104fa1f68abe103c57a815047c80916e340c9c031b3e7f0b2dbbb31e4de0234e19c273e3532f2fd8072c97e5361a2fe0a7100ab7ea55881b140253312001251088e18b97462173c5e1bb1c6d93cbddbe580b8f32b36d33410f64d89268cc3303b00'
```

### 16.13. TPM Attestation with ES256 Credential

[Registration](#registration-ceremony):

```
challenge = h'cfc82cdf1ceee876120aa88f0364f0910193460cfb97a317b2fe090694f9a299'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='tpm.ES256', L=32)

credential_private_key = h'80c60805e564f6d33e7abdff9d32e3db09a6219fe378a268d23107191b18e39f'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='tpm.ES256', L=32)
client_data_gen_flags = h'84'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='tpm.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
aaguid = h'4b92a377fc5f6107c4c85c190adbfd99'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='tpm.ES256', L=16)
credential_id = h'ec27bec7521c894bbb821105ea3724c90e770cf1fa354157ef18d0f18f78bea9'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='tpm.ES256', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'af'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='tpm.ES256', L=1)
attestation_private_key = h'6210f09e0ce7593e851a880a4bdde2d2192afeac46104abce1a890a5a71cf0c6'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='tpm.ES256', L=32)
attestation_cert_serial_number = h'311fc42da0ab10c43a9b1bf3a75e34e2'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='tpm.ES256', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a227a38677333787a753648595343716950413254776b51475452677a376c364d587376344a427054356f706b222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
attestationObject = h'a363666d746374706d6761747453746d74a663616c67266373696758463044022066e5826a652091030fd444e33c3eca2bc6dc548cf3045013addb38aa6457a21002203f3a5c95c9e707d0e555041bcc8698ee4ebc04e26cc8bae459705471789851766376657263322e30637835638159023a30820236308201dca0030201020210311fc42da0ab10c43a9b1bf3a75e34e2300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a30003059301306072a8648ce3d020106082a8648ce3d03010703420004c54e3f109094f60d7699b7db5d838569ffd1f3e1c9e897cd9eb40063f9402e3e9937e936cf1fcd5eb743ff443c97ab2edcd7c8e0e6cf6cfd413b8ab19fffa769a381d33081d0300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e041604145f546cb6973d4981e80fcdc7463859f5879680e4301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e30100603551d250409300706056781050803305e0603551d110101ff04543052a450304e314c3014060567810502010c0b69643a30303030303030303014060567810502030c0b69643a3030303030303030301e060567810502020c15576562417574686e207465737420766563746f7273300a06082a8648ce3d0403020348003045022063c9a2797b8066f1db34dd609f1ab6695607e7a98e9ff8090a68853c9a9fc949022100a55831a39f5b8a2aa9a68837829cabf43fea2a5cea4859ae851cac78e6ac3e97677075624172656158560023000b0004000000000010001000030010002041202698c9d9753fb4bb3f27cd09fe6b8afdb76438ee2ae54d7c9dade10d864b0020d8735115cdb330a63ea1d6e43d5000f4bd56f99bce83ee1d73301fc270116d076863657274496e666f5869ff544347801700000020277d0e05579dd013215a62273f7f3a3e7e191ead2654a3036d75a5a3ee37a6b0000000000000000011111111222222223300000000000000000022000b9c42d8aad5939331b9af3711af179f17123178098c9a7d0ca89fcd1fc800f3c7000068617574684461746158a4bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b54d000000004b92a377fc5f6107c4c85c190adbfd990020ec27bec7521c894bbb821105ea3724c90e770cf1fa354157ef18d0f18f78bea9a501020326200121582041202698c9d9753fb4bb3f27cd09fe6b8afdb76438ee2ae54d7c9dade10d864b225820d8735115cdb330a63ea1d6e43d5000f4bd56f99bce83ee1d73301fc270116d07'
```

[Authentication](#authentication-ceremony):

```
challenge = h'00093b66c21d5b5e89f7a07082118907ea3e502d343b314b8c5a54d62db202fb'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='tpm.ES256', L=32)

client_data_gen_flags = h'86'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='tpm.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'87'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='tpm.ES256', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50d00000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a2241416b375a7349645731364a393642776768474a422d6f2d554330304f7a464c6a46705531693279417673222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
signature = h'3045022060dc76b1607ec716c6e5eba8d056695ed6bc47b2e3d7a729c34e759e3ab66aa0022100d010a9e8fddcb64c439dfdca628ddb33cf245d567d157d9f66f942601bed9b38'
```

### 16.14. Android Key Attestation with ES256 Credential

[Registration](#registration-ceremony):

```
challenge = h'3de1f0b7365dccde3ff0cbf25e26ffa7baff87ef106c80fc865dc402d9960050'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='android-key.ES256', L=32)

credential_private_key = h'd4328d911acb0ebcc42aad29b29ffb55d5bc31d8af7ca9a16703d56c21abc7b4'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='android-key.ES256', L=32)
client_data_gen_flags = h'73'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='android-key.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'555d5c42e476a8b33f6a63dfa07ccbd2'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='android-key.ES256', L=16)
aaguid = h'ade9705e1ce7085b899a540d02199bf8'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='android-key.ES256', L=16)
credential_id = h'0a4729519788b6ed8a2d772b494e186244d8c798c052960dbc8c10c915176795'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='android-key.ES256', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'1e'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='android-key.ES256', L=1)
attestation_cert_serial_number = h'1ff91f76b63f44812f998b250b0286bf'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='android-key.ES256', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a2250654877747a5a647a4e345f384d76795869625f7037725f682d385162494438686c334541746d57414641222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a205656316351755232714c4d5f616d50666f487a4c30673d3d227d'
attestationObject = h'a363666d746b616e64726f69642d6b65796761747453746d74a363616c672663736967584630440220592bbc3c4c5f6158b52be1e085c92848986d7844245dfc9512e1a7e9ff7a2cd8022015bdd0852d3bd091e1c22da4211f4ccf0fdf4d912599d1c6630b1f310d3166f5637835638159026d3082026930820210a00302010202101ff91f76b63f44812f998b250b0286bf300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a305f311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d0301070342000499169657036d089a2a9821a7d0063d341f1a4613389359636efab5f3cbf1accfdd91c55543176ea99b644406dd1dd63774b6af65ac759e06ff40b1c8ab02df6ba381a83081a5300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e041604141ac81e50641e8d1339ab9f7eb25f0cd5aac054b0301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e3045060a2b06010401d679020111043730350202012c0201000201000201000420b20e943e3a7544b3a438943b6d5655313a47ef1af34e00ff3261aeb9ed155817040030003000300a06082a8648ce3d040302034700304402206f4609c9ffc946c418cef04c64a0d07bcce78f329b99270b822f2a4d1e3b75330220093c8d18328f36ef157f296393bdc7721dd2bd67438ffeaa42f051a044b7457168617574684461746158a4bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b55d00000000ade9705e1ce7085b899a540d02199bf800200a4729519788b6ed8a2d772b494e186244d8c798c052960dbc8c10c915176795a501020326200121582099169657036d089a2a9821a7d0063d341f1a4613389359636efab5f3cbf1accf225820dd91c55543176ea99b644406dd1dd63774b6af65ac759e06ff40b1c8ab02df6b'
```

[Authentication](#authentication-ceremony):

```
challenge = h'e4ee05ca9dbced74116540f24ed9adc62aae8507560522844ffa7eea14f7af86'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='android-key.ES256', L=32)

client_data_gen_flags = h'43'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='android-key.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'ab127107eff182bc3230beb5f1dad29c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='android-key.ES256', L=16)
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'4a'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0b', info='android-key.ES256', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50900000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a22354f344679703238375851525a55447954746d74786971756851645742534b45545f702d36685433723459222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a2071784a78422d5f78677277794d4c3631386472536e413d3d227d'
signature = h'304502202060107d953b286aa1bf35e3e8c78b383fddab5591b2db17ffb23ed83fe7df20022100a99be0297cb0d9d38aa96f30b760a4e0749dab385acd2a51d0560caae570d225'
```

### 16.15. Apple Anonymous Attestation with ES256 Credential

[Registration](#registration-ceremony):

```
challenge = h'f7f688213852007775009cf8c096fda89d60b9a9fb5a50dd81dd9898af5a0609'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='apple.ES256', L=32)

credential_private_key = h'de987bd9d43eeb44728ce0b14df11209dff931fb56b5b1948de4c0da1144ded0'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='apple.ES256', L=32)
client_data_gen_flags = h'5f'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='apple.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
extra_client_data = h'4e32cf9e939a5d052b14d71b1f6b5364'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='apple.ES256', L=16)
aaguid = h'748210a20076616a733b2114336fc384'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='apple.ES256', L=16)
credential_id = h'9c4a5886af9283d9be3e9ec55978dedfdce2e3b365cab193ae850c16238fafb8'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='apple.ES256', L=32)
; auth_data_UV_BE_BS determines the UV, BE and BS bits of the authenticator data flags, but BS is set only if BE is
auth_data_UV_BE_BS = h'2a'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='apple.ES256', L=1)
attestation_cert_serial_number = h'394275613d5310b81a29ce90f48b61c1'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='apple.ES256', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a22395f61494954685341486431414a7a34774a6239714a316775616e37576c4464676432596d4b396142676b222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73652c22657874726144617461223a22636c69656e74446174614a534f4e206d617920626520657874656e6465642077697468206164646974696f6e616c206669656c647320696e20746865206675747572652c207375636820617320746869733a20546a4c506e704f6158515572464e6362483274545a413d3d227d'
attestationObject = h'a363666d74656170706c656761747453746d74a1637835638159025c30820258308201fea0030201020210394275613d5310b81a29ce90f48b61c1300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a305f311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d030107034200048a3d5b1b4c543a706bf6e4b00afedb3c930b690dd286934fe2911f779cc7761af728e1aa3b0ff66692192daa776b83ddf8e3340d2d9a0eabdfc324eb3e2f136ca38196308193300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e0416041412f1ce6c0ae39b403bfc9200317bc183a4e4d766301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e303306092a864886f76364080204263024a122042097851a1a98b69c0614b26a94b70ec3aa07c061f89dbee23fbee01b6c42d718b0300a06082a8648ce3d040302034800304502207d541a5553f38b93b78b26a9dca58e64a7f8fac15ca206ae3ea32497cda375fb0221009137c6b75e767ec08224b29a7f703db4b745686dcc8a26b66e793688866d064f68617574684461746158a4bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b54900000000748210a20076616a733b2114336fc38400209c4a5886af9283d9be3e9ec55978dedfdce2e3b365cab193ae850c16238fafb8a50102032620012158208a3d5b1b4c543a706bf6e4b00afedb3c930b690dd286934fe2911f779cc7761a225820f728e1aa3b0ff66692192daa776b83ddf8e3340d2d9a0eabdfc324eb3e2f136c'
```

[Authentication](#authentication-ceremony):

```
challenge = h'd3eb2964641e26fed023403a72dde093b19c4ba9008c3f9dd83fcfd347a66d05'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='apple.ES256', L=32)

client_data_gen_flags = h'c2'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='apple.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'e2'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'0a', info='apple.ES256', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50900000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a22302d73705a4751654a76375149304136637433676b37476353366b416a442d6432445f503030656d625155222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
signature = h'3046022100ee35db795ce28044e1f8231d68b3d79a9882f7415aa35c1b5ac74d24251073c8022100dcc65691650a412d0ceef843710c09827acf26c7845bddac07eec95863e7fc4c'
```

### 16.16. FIDO U2F Attestation with ES256 Credential

[Registration](#registration-ceremony):

```
challenge = h'e074372990b9caa507a227dfc67b003780c45325380d1a90c20f81ed7d080c06'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'00', info='fido-u2f.ES256', L=32)

credential_private_key = h'51bd002938fa10b83683ac2a2032d0a7338c7f65a90228cfd1f61b81ec7288d0'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'01', info='fido-u2f.ES256', L=32)
client_data_gen_flags = h'00'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'02', info='fido-u2f.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
aaguid = h'afb3c2efc054df425013d5c88e79c3c1'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'03', info='fido-u2f.ES256', L=16)
credential_id = h'a4ba6e2d2cfec43648d7d25c5ed5659bc18f2b781538527ebd492de03256bdf4'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'04', info='fido-u2f.ES256', L=32)
attestation_private_key = h'66fda477a2a99d14c5edd7c1041a297ba5f3375108b1d032b79429f42349ce33'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'05', info='fido-u2f.ES256', L=32)
attestation_cert_serial_number = h'04f66dc6542ea7719dea416d325a2401'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'06', info='fido-u2f.ES256', L=16)

clientDataJSON = h'7b2274797065223a22776562617574686e2e637265617465222c226368616c6c656e6765223a22344851334b5a4335797155486f696666786e73414e344445557955344452715177672d4237583049444159222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
attestationObject = h'a363666d74686669646f2d7532666761747453746d74a26373696758473045022100f41887a20063bb26867cb9751978accea5b81791a68f4f4dd6ea1fb6a5c086c302204e5e00aa3895777e6608f1f375f95450045da3da57a0e4fd451df35a31d2d98a637835638159022530820221308201c7a003020102021004f66dc6542ea7719dea416d325a2401300a06082a8648ce3d0403023062311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331253023060355040b0c1c41757468656e74696361746f72204174746573746174696f6e204341310b30090603550406130241413020170d3234303130313030303030305a180f33303234303130313030303030305a305f311e301c06035504030c15576562417574686e207465737420766563746f7273310c300a060355040a0c0357334331223020060355040b0c1941757468656e74696361746f72204174746573746174696f6e310b30090603550406130241413059301306072a8648ce3d020106082a8648ce3d0301070342000456fffa7093dede46aefeefb6e520c7ccc78967636e2f92582ba71455f64e93932dff3be4e0d4ef68e3e3b73aa087e26a0a0a30b02dc2aa2309db4c3a2fc936dea360305e300c0603551d130101ff04023000300e0603551d0f0101ff040403020780301d0603551d0e04160414420822eb1908b5cd3911017fbcad4641c05e05a3301f0603551d2304183016801445aff715b0dd786741fee996ebc16547a3931b1e300a06082a8648ce3d040302034800304502200d0b777f0a0b181ad2830275acc3150fd6092430bcd034fd77beb7bdf8c2d546022100d4864edd95daa3927080855df199f1717299b24a5eecefbd017455a9b934d8f668617574684461746158a4bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b54100000000afb3c2efc054df425013d5c88e79c3c10020a4ba6e2d2cfec43648d7d25c5ed5659bc18f2b781538527ebd492de03256bdf4a5010203262001215820b0d62de6b30f86f0bac7a9016951391c2e31849e2e64661cbd2b13cd7d5508ad225820503b0bda2a357a9a4b34475a28e65b660b4898a9e3e9bbf0820d43494297edd0'
```

[Authentication](#authentication-ceremony):

```
challenge = h'f90c612981d84f599438de1a500f76926e92cc84bef8e02c6e23553f00485435'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'07', info='fido-u2f.ES256', L=32)

client_data_gen_flags = h'2c'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'08', info='fido-u2f.ES256', L=1)
; extra_client_data is included iff bit 0x01 of client_data_gen_flags is 1
; auth_data_UV_BS sets the UV and BS bits of the authenticator data flags, but BS is set only if BE was set in the registration
auth_data_UV_BS = h'd1'   ; Derived by: HKDF-SHA-256(IKM='WebAuthn test vectors', salt=h'09', info='fido-u2f.ES256', L=1)

authenticatorData = h'bfabc37432958b063360d3ad6461c9c4735ae7f8edd46592a5e0f01452b2e4b50100000000'
clientDataJSON = h'7b2274797065223a22776562617574686e2e676574222c226368616c6c656e6765223a222d5178684b59485954316d554f4e3461554139326b6d36537a49532d2d4f417362694e5650774249564455222c226f726967696e223a2268747470733a2f2f6578616d706c652e6f7267222c2263726f73734f726967696e223a66616c73657d'
signature = h'304402206172459958fea907b7292b92f555034bfd884895f287a76200c1ba287239137002204727b166147e26a21bbc2921d192ebfed569b79438538e5c128b5e28e6926dd7'
```

### 16.17.

This section lists example values for [WebAuthn extensions](#webauthn-extensions).

#### 16.17.1. Pseudo-random function extension (prf)

This section lists example values for the pseudo-random function ([prf](#prf)) extension.

Because the [prf](#prf) extension integrates with the CTAP2 `hmac-secret` extension [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)"), the examples are divided into two sections: example inputs and outputs for the WebAuthn [prf](#prf) extension, relevant to [WebAuthn Clients](#webauthn-client) and [WebAuthn Relying Parties](#webauthn-relying-party); and example mappings between the WebAuthn [prf](#prf) extension and the CTAP2 `hmac-secret` extension, relevant to [WebAuthn Clients](#webauthn-client) and [WebAuthn Authenticator](#webauthn-authenticator).

##### 16.17.1.1. Web Authentication API

The following examples may be used to test [WebAuthn Client](#webauthn-client) implementations of and [WebAuthn Relying Party](#webauthn-relying-party) usage of the [prf](#prf) extension. The examples are not exhaustive.

- The `enabled` output is always present during [registration ceremonies](#registration-ceremony), and never present during [authentication ceremonies](#authentication-ceremony):
	```
	// Example extension inputs:
	{ prf: {} }
	// Example client extension outputs from navigator.credentials.create():
	{ prf: { enabled: true } }
	{ prf: { enabled: false } }
	// Example client extension outputs from navigator.credentials.get():
	{ prf: {} }
	```
- The `results` output may be present during [registration ceremonies](#registration-ceremony) or [authentication ceremonies](#authentication-ceremony) if the `eval` or `evalByCredential` input is present:
	```
	// Example extension inputs:
	{ prf: { eval: { first: new Uint8Array([1, 2, 3, 4]) } } }
	// Example client extension outputs from navigator.credentials.create():
	{ prf: { enabled: true } }
	{ prf: { enabled: false } }
	{ prf: { enabled: true, results: { first: ArrayBuffer } } }
	// Example client extension outputs from navigator.credentials.get():
	{ prf: {} }
	{ prf: { results: { first: ArrayBuffer } } }
	```
- The `` `results`.`second` `` output is present if and only if the `results` output is present and the `` `second` `` input was present in the chosen PRF inputs:
	```
	// Example extension inputs:
	{
	  prf: {
	    eval: {
	      first: new Uint8Array([1, 2, 3, 4]),
	      second: new Uint8Array([5, 6, 7, 8]),
	    },
	    evalByCredential: {
	      "e02eZ9lPp0UdkF4vGRO4-NxlhWBkL1FCmsmb1tTfRyE": {
	        first: new Uint8Array([9, 10, 11, 12]),
	      }
	    }
	  }
	}
	// Example client extension outputs from navigator.credentials.get() if credential "e02eZ9lP..." was used:
	{ prf: { results: { first: ArrayBuffer } } }
	// Example client extension outputs from navigator.credentials.get() if a different credential was used:
	{ prf: {} }
	{ prf: { results: { first: ArrayBuffer, second: ArrayBuffer } } }
	```
- The `first` and `second` outputs may be any `BufferSource` type. Equal `first` and `second` inputs result in equal `first` and `second` outputs:
	```
	// Example extension inputs:
	{
	  prf: {
	    evalByCredential: {
	      "e02eZ9lPp0UdkF4vGRO4-NxlhWBkL1FCmsmb1tTfRyE": {
	        first: new Uint8Array([9, 10, 11, 12]),
	        second: new Uint8Array([9, 10, 11, 12])
	      }
	    }
	  }
	}
	// Example client extension outputs from navigator.credentials.get():
	{
	  prf: {
	    results: {
	      first: new Uint8Array([0xc4, 0x17, 0x2e, 0x98, 0x2e, 0x90, 0x97, 0xc3, 0x9a, 0x6c, 0x0c, 0xb7, 0x20, 0xcb, 0x37, 0x5b, 0x92, 0xe3, 0xfc, 0xad, 0x15, 0x4a, 0x63, 0xe4, 0x3a, 0x93, 0xf1, 0x09, 0x6b, 0x1e, 0x19, 0x73]),
	      second: new Uint32Array([0x982e17c4, 0xc397902e, 0xb70c6c9a, 0x5b37cb20, 0xadfce392, 0xe4634a15, 0x09f1933a, 0x73191e6b]),
	    }
	  }
	}
	```

Pseudo-random values used in this section were generated as follows:

- `"e02eZ9lPp0UdkF4vGRO4-NxlhWBkL1FCmsmb1tTfRyE" = Base64Url(SHA-256(UTF-8("WebAuthn PRF test vectors") || 0x00))`
- `h'c4172e982e9097c39a6c0cb720cb375b92e3fcad154a63e43a93f1096b1e1973' = SHA-256(UTF-8("WebAuthn PRF test vectors") || 0x01)`

##### 16.17.1.2. CTAP2 hmac-secret extension

The following examples may be used to test [WebAuthn Client](#webauthn-client) implementations of how the [prf](#prf) extension uses the [\[FIDO-CTAP\]](#biblio-fido-ctap "Client to Authenticator Protocol (CTAP)") `hmac-secret` extension. The examples are given in CDDL [\[RFC8610\]](#biblio-rfc8610 "Concise Data Definition Language (CDDL): A Notational Convention to Express Concise Binary Object Representation (CBOR) and JSON Data Structures") notation. The examples are not exhaustive.

- The following shared definitions are used in all subsequent examples:
	```
	; Given input parameters:
	platform_key_agreement_private_key = 0x0971bc7fb1be48270adcd3d9a5fc15d5fb0f335b3071ff36a54c007fa6c76514
	authenticator_key_agreement_public_key = {
	    1: 2,
	    3: -25,
	    -1: 1,
	    -2: h'a30522c2de402b561965c3cf949a1cab020c6f6ea36fcf7e911ac1a0f1515300',
	    -3: h'9961a929abdb2f42e6566771887d41484d889e735e3248518a53112d2b915f00',
	}
	authenticator_cred_random = h'437e065e723a98b2f08f39d8baf7c53ecb3c363c5e5104bdaaf5d5ca2e028154'
	```
	The `first` and `second` inputs are mapped in the examples as `prf_eval_first` and `prf_eval_second`, respectively. The `prf_results_first` and `prf_results_second` values in the examples are mapped to the `` `results`.`first` `` and `` `results`.`second` `` outputs, respectively.
- Single input case using PIN protocol 2:
	```
	; Inputs from Relying Party:
	prf_eval_first = h'576562417574686e20505246207465737420766563746f727302'
	; Client computes:
	shared_secret = h'0c63083de8170101d38bcf8bd72309568ddb4550867e23404b35d85712f7c20d8bc911ee23c06034cbc14290b9669bec07739053c5a416e313ef905c79955876'
	salt1 = h'527413ebb48293772df30f031c5ac4650c7de14bf9498671ae163447b6a772b3'
	salt_enc = h'23dde5e3462daf36559b85c4ac5f9656aa9bfd81c1dc2bf8533c8b9f3882854786b4f500e25b4e3d81f7fc7c74236229'
	; Authenticator computes:
	output1 = h'3c33e07d202c3b029cc21f1722767021bf27d595933b3d2b6a1b9d5dddc77fae'
	output_enc = h'3bfaa48f7952330d63e35ff8cd5bca48d2a12823828915749287256ab146272f9fb437bf65691243c3f504bd7ea6d5e6'
	; Client decrypts:
	prf_results_first = h'3c33e07d202c3b029cc21f1722767021bf27d595933b3d2b6a1b9d5dddc77fae'
	```
- Two input case using PIN protocol 2:
	```
	; Inputs from Relying Party:
	prf_eval_first = h'576562417574686e20505246207465737420766563746f727302'
	prf_eval_second = h'576562417574686e20505246207465737420766563746f727303'
	; Client computes:
	shared_secret = h'0c63083de8170101d38bcf8bd72309568ddb4550867e23404b35d85712f7c20d8bc911ee23c06034cbc14290b9669bec07739053c5a416e313ef905c79955876'
	salt1 = h'527413ebb48293772df30f031c5ac4650c7de14bf9498671ae163447b6a772b3'
	salt2 = h'd68ac03329a10ee5e0ec834492bb9a96a0e547baf563bf78ccbe8789b22e776b'
	salt_enc = h'd9f4236403e0fe843a8e4e5be764d120904c198ad6e77b089876a3391961f183b0008b4ca66b91cd72aa35b6151ff981f6e5649f3c040e6615ad7dd8ae96ef23b229a5c97c3f0dcd8605eee166ce163a'
	; Authenticator computes:
	output1 = h'3c33e07d202c3b029cc21f1722767021bf27d595933b3d2b6a1b9d5dddc77fae'
	output2 = h'a62a8773b19cda90d7ed4ef72a80a804320dbd3997e2f663805ad1fd3293d50b'
	output_enc = h'90ee52f739043bc17b3488a74306d7801debb5b61f18662c648a25b5b5678ede482cdaff99a537a44f064fcb10ce6e04dfd27619dc96a0daff8507e499296b1eecf0981f7c8518b277a7a3018f5ec6fb'
	; Client decrypts:
	prf_results_first = h'3c33e07d202c3b029cc21f1722767021bf27d595933b3d2b6a1b9d5dddc77fae'
	prf_results_second = h'a62a8773b19cda90d7ed4ef72a80a804320dbd3997e2f663805ad1fd3293d50b'
	```
- Single input case using PIN protocol 1:
	```
	; Inputs from Relying Party:
	prf_eval_first = h'576562417574686e20505246207465737420766563746f727302'
	; Client computes:
	shared_secret = h'23e5ed7157c25892b77732fb9c8a107e3518800db2af4142f9f4adfacb771d39'
	salt1 = h'527413ebb48293772df30f031c5ac4650c7de14bf9498671ae163447b6a772b3'
	salt_enc = h'ab8c878bb05d04700f077ed91845ec9c503c925cb12b327ddbeb4243c397f913'
	; Authenticator computes:
	output1 = h'3c33e07d202c3b029cc21f1722767021bf27d595933b3d2b6a1b9d5dddc77fae'
	output_enc = h'15d4e4f3f04109b492b575c1b38c28585b6719cf8d61304215108d939f37ccfb'
	; Client decrypts:
	prf_results_first = h'3c33e07d202c3b029cc21f1722767021bf27d595933b3d2b6a1b9d5dddc77fae'
	```

Inputs and pseudo-random values used in this section were generated as follows:

- `seed = UTF-8("WebAuthn PRF test vectors")`
- `prf_eval_first = seed || 0x02`
- `prf_eval_second = seed || 0x03`
- `platform_key_agreement_private_key = SHA-256(seed || 0x04)`
- `authenticator_key_agreement_public_key = P256-Public-Key(sk)` where `sk = SHA-256(seed || 0x05)`
- `authenticator_cred_random = SHA-256(seed || 0x06)`
- `iv` in single-input `salt_enc` with PIN protocol 2: Truncated `SHA-256(seed || 0x07)`
- `iv` in two-input `salt_enc` with PIN protocol 2: Truncated `SHA-256(seed || 0x08)`
- `iv` in single-input `output_enc` with PIN protocol 2: Truncated `SHA-256(seed || 0x09)`
- `iv` in two-input `output_enc` with PIN protocol 2: Truncated `SHA-256(seed || 0x0a)`

## 17\. Acknowledgements

We thank the following people for their reviews of, and contributions to, this specification: Yuriy Ackermann, James Barclay, Richard Barnes, Dominic Battré, Julien Cayzac, Domenic Denicola, Rahul Ghosh, Brad Hill, Nidhi Jaju, Jing Jin, Wally Jones, Ian Kilpatrick, Axel Nennker, Zack Newman, Yoshikazu Nojima, Kimberly Paulhamus, Adam Powers, Yaron Sheffer, Anne van Kesteren, Johan Verrept, and Boris Zbarsky.

Thanks to Adam Powers for creating the overall [registration](#registration) and [authentication](#authentication) flow diagrams ([Figure 1](#fig-registration) and [Figure 2](#fig-authentication)).

We thank Anthony Nadalin, John Fontana, and Richard Barnes for their contributions as co-chairs of the [Web Authentication Working Group](https://www.w3.org/Webauthn/).

We also thank Simone Onofri, Philippe Le Hégaret, Wendy Seltzer, Samuel Weiler, and Harry Halpin for their contributions as our W3C Team Contacts.

## 18\. Revision History

*This section is not normative.*

This section summarizes the significant changes that have been made to this specification over time.

### 18.1.

*These changes will be merged into the next section when finalizing Level 3. Changes to content that was not yet present in Level 2 are listed with a leading "(\*)" mark and will then be deleted from the merged change history.*

Normative changes:

- (\*) Added dictionary extensions to `AuthenticationExtensionsClientInputsJSON` and `AuthenticationExtensionsClientOutputsJSON` in definitions of extensions.
- Added recommendation against using `COSEAlgorithmIdentifier` values -9, -51, -52 and -19 in `pubKeyCredParams`.
- Added requirement for ESP256 (-9), ESP384 (-51) and ESP512 (-52) public keys to use uncompressed form: [§ 5.8.5 Cryptographic Algorithm Identifier (typedef COSEAlgorithmIdentifier)](#sctn-alg-identifier)

Editorial changes:

- (\*) Fixed section heading levels of test vectors subsections: [Web Authentication: An API for accessing Public Key Credentials - Level 3 § sctn-test-vectors](https://www.w3.org/TR/2025/WD-webauthn-3-20250127/#sctn-test-vectors)
- Removed outdated notes about permissions policy in [Web Authentication: An API for accessing Public Key Credentials - Level 3 § sctn-isUserVerifyingPlatformAuthenticatorAvailable](https://www.w3.org/TR/2025/WD-webauthn-3-20250127/#sctn-isUserVerifyingPlatformAuthenticatorAvailable) and [Web Authentication: An API for accessing Public Key Credentials - Level 3 § sctn-getClientCapabilities](https://www.w3.org/TR/2025/WD-webauthn-3-20250127/#sctn-getClientCapabilities).
- Added algorithm -8 (EdDSA) to example code in [Web Authentication: An API for accessing Public Key Credentials - Level 3 § sctn-sample-registration](https://www.w3.org/TR/2025/WD-webauthn-3-20250127/#sctn-sample-registration).
- (\*) Clarified meaning of `prf` extension output `enabled`: [Web Authentication: An API for accessing Public Key Credentials - Level 3 § dom-authenticationextensionsprfoutputs-enabled](https://www.w3.org/TR/2025/WD-webauthn-3-20250127/#dom-authenticationextensionsprfoutputs-enabled)
- (\*) Fixed mistake in how test vectors were generated in [Web Authentication: An API for accessing Public Key Credentials - Level 3 § test-vectors-extensions-prf-ctap](https://www.w3.org/TR/2025/WD-webauthn-3-20250127/#test-vectors-extensions-prf-ctap).
- (\*) Changed Ed25519 test vectors to be generated from the seed `'packed.EdDSA'` instead of `'packed.Ed25519'`: [§ 16.11 Packed Attestation with Ed25519 Credential](#sctn-test-vectors-packed-eddsa)
- (\*) Added Ed448 test vectors: [§ 16.12 Packed Attestation with Ed448 Credential](#sctn-test-vectors-packed-ed448)
- Changed DER example in [§ 6.5.5 Signature Formats for Packed Attestation, FIDO U2F Attestation, and Assertion Signatures](#sctn-signature-attestation-types) to include INTEGER components of differing lengths.

### 18.2.

#### 18.2.1. Substantive Changes

The following changes were made to the [Web Authentication API](#web-authentication-api) and the way it operates.

Changes:

- Updated timeout guidance: [§ 15.1 Recommended Range for Ceremony Timeouts](#sctn-timeout-recommended-range)
- `uvm` extension no longer included; see instead L2 [\[webauthn-2-20210408\]](#biblio-webauthn-2-20210408 "Web Authentication: An API for accessing Public Key Credentials - Level 2").
- [aaguid](#authdata-attestedcredentialdata-aaguid) in [attested credential data](#attested-credential-data) is no longer zeroed when `attestation` preference is `none`: [§ 5.1.3 Create a New Credential - PublicKeyCredential’s \[\[Create\]\](origin, options, sameOriginWithAncestors) Internal Method](#sctn-createCredential)

Deprecations:

- Registration parameter `` `publicKey`.`rp`.`name` ``: [§ 5.4.1 Public Key Entity Description (dictionary PublicKeyCredentialEntity)](#dictionary-pkcredentialentity)
- [§ 8.5 Android SafetyNet Attestation Statement Format](#sctn-android-safetynet-attestation)
- [tokenBinding](#dom-collectedclientdata-tokenbinding) was changed to \[RESERVED\].
- In-field language and direction metadata are no longer recommended:
	- [§ 6.4.2 Language and Direction Encoding](#sctn-strings-langdir)
		- `` `publicKey`.`rp`.`name` ``
		- `` `publicKey`.`user`.`name` ``
		- `` `publicKey`.`user`.`displayName` ``

New features:

- New JSON (de)serialization methods:
	- `toJSON()` method in [§ 5.1 PublicKeyCredential Interface](#iface-pkcredential)
		- [§ 5.1.8 Deserialize Registration ceremony options - PublicKeyCredential’s parseCreationOptionsFromJSON() Method](#sctn-parseCreationOptionsFromJSON)
		- [§ 5.1.9 Deserialize Authentication ceremony options - PublicKeyCredential’s parseRequestOptionsFromJSON() Methods](#sctn-parseRequestOptionsFromJSON)
- Create operations in cross-origin iframes:
	- [§ 5.1.3 Create a New Credential - PublicKeyCredential’s \[\[Create\]\](origin, options, sameOriginWithAncestors) Internal Method](#sctn-createCredential)
		- [§ 5.10 Using Web Authentication within iframe elements](#sctn-iframe-guidance)
- Conditional mediation for create: [§ 5.1.3 Create a New Credential - PublicKeyCredential’s \[\[Create\]\](origin, options, sameOriginWithAncestors) Internal Method](#sctn-createCredential)
- Conditional mediation for get: [§ 5.1.4 Use an Existing Credential to Make an Assertion](#sctn-getAssertion)
- [§ 5.1.7 Availability of client capabilities - PublicKeyCredential’s getClientCapabilities() Method](#sctn-getClientCapabilities)
	- [§ 14.5.4 Disclosing Client Capabilities](#sctn-disclosing-client-capabilities)
- New enum value `hybrid` in [§ 5.8.4 Authenticator Transport Enumeration (enum AuthenticatorTransport)](#enum-transport).
- [§ 5.1.10 Signal Credential Changes to the Authenticator - PublicKeyCredential’s signal methods](#sctn-signal-methods)
- New [client data](#client-data) attribute `topOrigin`: [§ 5.8.1 Client Data Used in WebAuthn Signatures (dictionary CollectedClientData)](#dictionary-client-data)
- [§ 5.8.8 User-agent Hints Enumeration (enum PublicKeyCredentialHint)](#enum-hints)
- [§ 5.11 Using Web Authentication across related origins](#sctn-related-origins)
- [Authenticator data](#authenticator-data) flags [BE](#authdata-flags-be) and [BS](#authdata-flags-bs) assigned:
	- [§ 6.1 Authenticator Data](#sctn-authenticator-data)
		- [§ 6.1.3 Credential Backup State](#sctn-credential-backup)
		- [§ 11.10 Set Credential Properties](#sctn-automation-set-credential-properties)
- [§ 8.9 Compound Attestation Statement Format](#sctn-compound-attestation)
- [§ 10.1.4 Pseudo-random function extension (prf)](#prf-extension)
- Registration parameter `` `publicKey`.`attestationFormats` ``: [§ 5.4 Options for Credential Creation (dictionary PublicKeyCredentialCreationOptions)](#dictionary-makecredentialoptions)

#### 18.2.2. Editorial Changes

The following changes were made to improve clarity, readability, navigability and similar aspects of the document.

- Updated [§ 1.2 Use Cases](#sctn-use-cases) to reflect developments in deployment landscape.
- Introduced [credential record](#credential-record) concept to formalize what data [Relying Parties](#relying-party) need to store and how it relates between [registration](#registration-ceremony) and [authentication ceremonies](#authentication-ceremony).
- Clarified error conditions:
	- [§ 5.1.3.1 Create Request Exceptions](#sctn-create-request-exceptions)
		- [§ 5.1.4.3 Get Request Exceptions](#sctn-get-request-exceptions)
- [§ 6.4 String Handling](#sctn-strings) split into subsections [§ 6.4.1.1 String Truncation by Clients](#sctn-strings-truncation-client) and [§ 6.4.1.2 String Truncation by Authenticators](#sctn-strings-truncation-authenticator) to clarify division of responsibilities.
- Added [§ 16 Test Vectors](#sctn-test-vectors).
- Moved normative language outside of "note" blocks.