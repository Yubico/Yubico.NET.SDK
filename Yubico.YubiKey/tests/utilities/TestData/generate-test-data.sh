#!/bin/bash

# Function to generate RSA keys and certificates
generate_rsa() {
    local bits=$1
    echo "Generating RSA-$bits keys and certificates..."

    # Generate private key
    openssl genrsa -out rsa${bits}_private.pem $bits

    # Generate public key
    openssl rsa -in rsa${bits}_private.pem -pubout -out rsa${bits}_public.pem

    # Generate CSR
    openssl req -new -key rsa${bits}_private.pem -out rsa${bits}.csr -subj "/C=US/ST=State/L=City/O=Organization/OU=Unit/CN=rsa${bits}.example.com"

    # Generate self-signed certificate
    openssl x509 -req -days 36500 -in rsa${bits}.csr -signkey rsa${bits}_private.pem -out rsa${bits}_cert.pem

    # Generate certificate with attestation
    openssl x509 -req -days 36500 -in rsa${bits}.csr -signkey rsa${bits}_private.pem -out rsa${bits}_cert_attest.pem \
        -extfile <(printf "keyUsage=digitalSignature,keyEncipherment\nsubjectKeyIdentifier=hash\nauthorityKeyIdentifier=keyid:always,issuer\nbasicConstraints=CA:TRUE")
}

# Function to generate EC keys and certificates
generate_ec() {
    local curve=$1
    local curve_param

    case $curve in
        "P256") curve_param="prime256v1" ;;
        "P384") curve_param="secp384r1" ;;
        "P521") curve_param="secp521r1" ;;
        "ED25519") curve_param="ed25519" ;;
        "X25519") curve_param="x25519" ;;
        *) echo "Invalid curve"; return 1 ;;
    esac

    echo "Generating $curve keys and certificates..."

    if [[ $curve == "ED25519" || $curve == "X25519" ]]; then
        # Generate private key
        openssl genpkey -algorithm $curve_param -out ${curve,,}_private.pem

        # Generate public key
        openssl pkey -in ${curve,,}_private.pem -pubout -out ${curve,,}_public.pem
        
        if [[ $curve == "ED25519" ]]; then
          # Generate CSR
          openssl req -new -key ${curve,,}_private.pem -out ${curve,,}.csr -subj "/C=US/ST=State/L=City/O=Organization/OU=Unit/CN=${curve,,}.example.com"
      
          # Generate self-signed certificate
          openssl x509 -req -days 36500 -in ${curve,,}.csr -signkey ${curve,,}_private.pem -out ec${curve,,}_cert.pem
      
          # Generate certificate with attestation
          openssl x509 -req -days 36500 -in ${curve,,}.csr -signkey ${curve,,}_private.pem -out ec${curve,,}_cert_attest.pem \
              -extfile <(printf "keyUsage=digitalSignature,keyEncipherment\nsubjectKeyIdentifier=hash\nauthorityKeyIdentifier=keyid:always,issuer\nbasicConstraints=CA:TRUE")
        fi
    else
        # Generate private key in PKCS8 format
        openssl ecparam -name $curve_param -genkey | openssl pkcs8 -topk8 -nocrypt -out ec${curve,,}_private.pem

        # Generate public key
        openssl ec -in ${curve,,}_private.pem -pubout -out ec${curve,,}_public.pem
        
        # Generate CSR
        openssl req -new -key ${curve,,}_private.pem -out ${curve,,}.csr -subj "/C=US/ST=State/L=City/O=Organization/OU=Unit/CN=${curve,,}.example.com"
    
        # Generate self-signed certificate
        openssl x509 -req -days 36500 -in ${curve,,}.csr -signkey ${curve,,}_private.pem -out ec${curve,,}_cert.pem
    
        # Generate certificate with attestation
        openssl x509 -req -days 36500 -in ${curve,,}.csr -signkey ${curve,,}_private.pem -out ec${curve,,}_cert_attest.pem \
            -extfile <(printf "keyUsage=digitalSignature,keyEncipherment\nsubjectKeyIdentifier=hash\nauthorityKeyIdentifier=keyid:always,issuer\nbasicConstraints=CA:TRUE")
    fi
}

# Create directory for keys and certificates
mkdir -p crypto_keys
cd crypto_keys

# Generate RSA keys and certificates
for bits in 1024 2048 3072 4096; do
    generate_rsa $bits
done

# Generate EC keys and certificates
for curve in P256 P384 P521 ED25519 X25519; do
    generate_ec $curve
done

echo "All keys and certificates have been generated in the crypto_keys directory."
