package main

import (
	"archive/zip"
	"strings"
	"testing"
)

func TestValidateRequestedNameRejectsUnsafeNames(t *testing.T) {
	for _, test := range []struct{ name, want string }{
		{"", "empty"},
		{`lib\net47\Yubico.Core.dll`, "backslash"},
		{"/lib/Yubico.Core.dll", "absolute"},
		{"C:/lib/Yubico.Core.dll", "drive"},
		{"lib/../../etc/passwd", "dot path segment"},
		{"./lib/Yubico.Core.dll", "dot path segment"},
		{"lib//Yubico.Core.dll", "empty path segment"},
		{"lib/net47/", "empty path segment"},
	} {
		t.Run(test.name, func(t *testing.T) {
			err := validateRequestedName(test.name)
			if err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("validateRequestedName(%q) = %v, want %q", test.name, err, test.want)
			}
		})
	}
}

func TestValidateRequestedNameAcceptsOrdinaryPackagePaths(t *testing.T) {
	for _, name := range []string{
		"lib/net47/Yubico.Core.dll",
		"lib/netstandard2.0/Yubico.YubiKey.dll",
		"runtimes/win-x64/native/Yubico.NativeShims.dll",
	} {
		if err := validateRequestedName(name); err != nil {
			t.Fatalf("validateRequestedName(%q) = %v", name, err)
		}
	}
}

func TestSelectEntriesReturnsRequestedOrder(t *testing.T) {
	path := writeNupkg(t,
		packageEntry{name: "lib/b.dll", contents: []byte("b")},
		packageEntry{name: "lib/a.dll", contents: []byte("a")},
		packageEntry{name: "readme.txt", contents: []byte("untouched")},
	)
	archive, err := zip.OpenReader(path)
	if err != nil {
		t.Fatal(err)
	}
	defer archive.Close()

	selected, err := selectEntries(&archive.Reader, []string{"lib/a.dll", "lib/b.dll"})
	if err != nil {
		t.Fatal(err)
	}
	if len(selected) != 2 {
		t.Fatalf("selected %d entries, want 2", len(selected))
	}
	if selected[0].name != "lib/a.dll" || selected[1].name != "lib/b.dll" {
		t.Fatalf("selection order is %s, %s", selected[0].name, selected[1].name)
	}
	if string(selected[0].contents) != "a" || string(selected[1].contents) != "b" {
		t.Fatal("selected entries carry the wrong contents")
	}
}

func TestSelectEntriesRejectsDuplicateRequests(t *testing.T) {
	path := writeNupkg(t, packageEntry{name: "lib/a.dll", contents: []byte("a")})
	archive, err := zip.OpenReader(path)
	if err != nil {
		t.Fatal(err)
	}
	defer archive.Close()

	for _, requested := range [][]string{
		{"lib/a.dll", "lib/a.dll"},
		{"lib/a.dll", "LIB/A.DLL"},
	} {
		_, err := selectEntries(&archive.Reader, requested)
		if err == nil || !strings.Contains(err.Error(), "duplicate --assembly") {
			t.Fatalf("selectEntries(%v) = %v, want a duplicate request error", requested, err)
		}
	}
}

func TestSelectEntriesRejectsUnsafeRequest(t *testing.T) {
	path := writeNupkg(t, packageEntry{name: "lib/a.dll", contents: []byte("a")})
	archive, err := zip.OpenReader(path)
	if err != nil {
		t.Fatal(err)
	}
	defer archive.Close()

	_, err = selectEntries(&archive.Reader, []string{"../lib/a.dll"})
	if err == nil || !strings.Contains(err.Error(), "unsafe --assembly") {
		t.Fatalf("expected unsafe request error, got %v", err)
	}
}

func TestSelectEntriesRejectsUnmatchedRequest(t *testing.T) {
	path := writeNupkg(t, packageEntry{name: "lib/a.dll", contents: []byte("a")})
	archive, err := zip.OpenReader(path)
	if err != nil {
		t.Fatal(err)
	}
	defer archive.Close()

	// A request that differs only by case must not silently match: the ZIP
	// name is the exact name the signer wrote.
	for _, requested := range []string{"lib/missing.dll", "lib/A.dll"} {
		_, err := selectEntries(&archive.Reader, []string{requested})
		if err == nil || !strings.Contains(err.Error(), "no ZIP entry named "+requested) {
			t.Fatalf("selectEntries(%q) = %v, want an unmatched error", requested, err)
		}
	}
}

func TestSelectEntriesRejectsHostileArchiveNames(t *testing.T) {
	for name, entries := range map[string][]packageEntry{
		"duplicate": {
			{name: "lib/a.dll", contents: []byte("a")},
			{name: "lib/a.dll", contents: []byte("shadow")},
		},
		"case-insensitive duplicate": {
			{name: "lib/a.dll", contents: []byte("a")},
			{name: "LIB/A.DLL", contents: []byte("shadow")},
		},
		"traversal": {
			{name: "lib/a.dll", contents: []byte("a")},
			{name: "../escape.txt", contents: []byte("escape")},
		},
		"absolute": {
			{name: "lib/a.dll", contents: []byte("a")},
			{name: "/etc/passwd", contents: []byte("escape")},
		},
		"backslash": {
			{name: "lib/a.dll", contents: []byte("a")},
			{name: `lib\b.dll`, contents: []byte("escape")},
		},
	} {
		t.Run(name, func(t *testing.T) {
			path := writeNupkg(t, entries...)
			archive, err := zip.OpenReader(path)
			if err != nil {
				t.Fatal(err)
			}
			defer archive.Close()
			if _, err := selectEntries(&archive.Reader, []string{"lib/a.dll"}); err == nil {
				t.Fatalf("accepted an archive containing %s names", name)
			}
		})
	}
}

// Directory entries are legal in a ZIP and harmless, so scanning the archive
// must not reject a package just for containing one.
func TestSelectEntriesAllowsDirectoryEntries(t *testing.T) {
	path := writeNupkg(t,
		packageEntry{name: "lib/"},
		packageEntry{name: "lib/a.dll", contents: []byte("a")},
	)
	archive, err := zip.OpenReader(path)
	if err != nil {
		t.Fatal(err)
	}
	defer archive.Close()

	if _, err := selectEntries(&archive.Reader, []string{"lib/a.dll"}); err != nil {
		t.Fatal(err)
	}
}
