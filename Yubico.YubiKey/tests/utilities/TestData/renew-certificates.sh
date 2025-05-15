#!/bin/bash

# Script to regenerate certificates with 36500-day validity from existing keys

# Function to regenerate RSA certificates
regenerate_rsa_cert() {
    local bits=$1
    echo "Regenerating RSA-$bits certificates with 36500 days validity..."
    
    # Generate CSR
    openssl req -new -key rsa${bits}_private.pem -out rsa${bits}.csr -subj "/C=US/ST=State/L=City/O=Organization/OU=Unit/CN=rsa${bits}.example.com"
    
    # Generate self-signed certificate with 36500 days validity and proper CA extensions
    openssl x509 -req -days 36500 -in rsa${bits}.csr -signkey rsa${bits}_private.pem -out rsa${bits}_cert.pem \
        -extfile <(printf "basicConstraints=critical,CA:TRUE\nkeyUsage=critical,keyCertSign,cRLSign\nsubjectKeyIdentifier=hash\nauthorityKeyIdentifier=keyid:always,issuer")
    
    # Generate certificate with attestation
    openssl x509 -req -days 36500 -in rsa${bits}.csr -signkey rsa${bits}_private.pem -out rsa${bits}_cert_attest.pem \
        -extfile <(printf "basicConstraints=critical,CA:TRUE\nkeyUsage=critical,digitalSignature,keyEncipherment,keyCertSign,cRLSign\nsubjectKeyIdentifier=hash\nauthorityKeyIdentifier=keyid:always,issuer")
}

# Function to regenerate EC certificates
regenerate_ec_cert() {
    local curve=$1
    echo "Regenerating $curve certificates with 36500 days validity..."
    
    local keyfile
    local certprefix
    
    # Skip X25519 as it doesn't support certificates
    if [[ $curve == "X25519" ]]; then
        echo "Skipping X25519 (doesn't support certificates)..."
        return
    fi
    
    # Handle different naming conventions for keys
    if [[ $curve == "ED25519" ]]; then
        keyfile="${curve,,}_private.pem"
        certprefix="${curve,,}"
    else
        keyfile="ec${curve,,}_private.pem"
        certprefix="ec${curve,,}"
    fi
    
    # Generate CSR
    openssl req -new -key $keyfile -out ${curve,,}.csr -subj "/C=US/ST=State/L=City/O=Organization/OU=Unit/CN=${curve,,}.example.com"
    
    # Generate self-signed certificate with proper CA extensions
    openssl x509 -req -days 36500 -in ${curve,,}.csr -signkey $keyfile -out ${certprefix}_cert.pem \
        -extfile <(printf "basicConstraints=critical,CA:TRUE\nkeyUsage=critical,keyCertSign,cRLSign\nsubjectKeyIdentifier=hash\nauthorityKeyIdentifier=keyid:always,issuer")
    
    # Generate certificate with attestation
    openssl x509 -req -days 36500 -in ${curve,,}.csr -signkey $keyfile -out ${certprefix}_cert_attest.pem \
        -extfile <(printf "basicConstraints=critical,CA:TRUE\nkeyUsage=critical,digitalSignature,keyEncipherment,keyCertSign,cRLSign\nsubjectKeyIdentifier=hash\nauthorityKeyIdentifier=keyid:always,issuer")
}

# Assume we're already in the crypto_keys directory or adapt as needed
cd crypto_keys 2>/dev/null || true

# Regenerate RSA certificates
for bits in 1024 2048 3072 4096; do
    regenerate_rsa_cert $bits
done

# Regenerate EC certificates
for curve in P256 P384 P521 ED25519; do
    regenerate_ec_cert $curve
done

# Also handle X25519 (though it doesn't use certificates)
regenerate_ec_cert X25519

echo "All certificates have been regenerated with 36500 days validity and proper CA extensions."