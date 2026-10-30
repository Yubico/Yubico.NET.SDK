package main

import (
	"os"
	"strings"
)

// Start with an allow-list, not the release shell's credential collection.
// Names are compared case-insensitively for Windows; original spelling stays.
func childEnvironment(purpose string) []string {
	allowed := map[string]bool{}
	for _, name := range strings.Fields(`PATH HOME USER USERNAME USERPROFILE
		XDG_CONFIG_HOME XDG_CACHE_HOME XDG_DATA_HOME
		APPDATA LOCALAPPDATA SYSTEMROOT WINDIR COMSPEC PATHEXT
		TMP TMPDIR TEMP LANG LC_ALL LC_CTYPE TERM
		HTTP_PROXY HTTPS_PROXY ALL_PROXY NO_PROXY SSL_CERT_FILE SSL_CERT_DIR
		PCSCLITE_CSOCK_NAME`) {
		allowed[name] = true
	}
	if purpose == "attest" {
		for _, name := range strings.Fields("GH_TOKEN GITHUB_TOKEN GH_ENTERPRISE_TOKEN GITHUB_ENTERPRISE_TOKEN GH_HOST GH_CONFIG_DIR") {
			allowed[name] = true
		}
	}
	if purpose == "sign" {
		// Compatibility for file/KMS identities. PINs always use the pipe.
		for _, name := range strings.Fields("NUGET_SIGN_PASSWORD GOOGLE_APPLICATION_CREDENTIALS GOOGLE_CLOUD_PROJECT CLOUDSDK_CONFIG") {
			allowed[name] = true
		}
	}
	result := []string{}
	for _, entry := range os.Environ() {
		name, _, _ := strings.Cut(entry, "=")
		if allowed[strings.ToUpper(name)] {
			result = append(result, entry)
		}
	}
	return result
}
