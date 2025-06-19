# 🦇 BSCP

**BSCP v2** — *Binary Secure Communication Protocol* &#x20;
Also known internally as: **BatShit Crazy Protocol**

This is a protocol so secure, so structured, and so fundamentally cursed that it not only validates messages — it judges them.

---

## 🔐 RSA & AES in BSCP v2

BSCP v2 brings asymmetric and symmetric cryptography into the madness.

### CrazyRSA.cs — RSA Key Exchange

* RSA public keys are sent wrapped in layers of checksum, padding, and start/end markers (naturally).
* Keys are exchanged once during handshake to bootstrap trust.
* Each key is padded and checksumed, split across start and end bytes just to make parsing harder.
* After unpacking and checksum validation, the pure RSA key is imported for encryption/decryption.

### AES Session Keys

* AES-256 keys and IVs are generated freshly per session.
* Keys and IVs are also checksumed and wrapped in signature BSCP style.
* The IV is sent once securely via RSA handshake, then reused for all AES-encrypted messages that session.
* AES uses CBC mode with PKCS7 padding to handle arbitrary-length data.

### Encryption Flow

1. RSA handshake to exchange public keys (using the new `CrazyRSA.cs`).
2. AES key + IV sent wrapped in checksum and markers, and sent through RSA.
3. All subsequent messages are encrypted with AES.
4. Every message includes BSCP’s signature checksums and pudding bits for integrity and style.

---

## 🧠 What is BSCP?

BSCP is an experimental communication protocol that obeys no standard but its own.

It includes:

* 🧩 **21-bit handshakes** with aesthetically centered padding
* 🧵 **12-bit string characters** with built-in 4-bit checksums
* 💌 **Client messages** marked with a trailing `1` bit, for identity (and misalignment)
* 🎭 **ReceiveStatus** values ranging from `1 = Success` to `-153 = Please never contact me again`
* 🧮 **Checksum logic** that’s emotionally derived from message length, the first and last character, and pure spite
* 🛡️ Now includes **asymmetric key exchange** in v2, because why not?

---

## 📁 Structure

```text
BSCPTester/
├── CommonAPI/
│   ├── CrazyHandshake.cs          # Handles cursed 21-bit +1 handshakes
│   ├── CrazyCharacter.cs          # Home of StupidMicrosoftAsciiChar (12-bit + checksum + client flag)
│   ├── CrazyReceiveStatus.cs      # Emotional responses (e.g. -153)
│   ├── CrazyTwelveBitNumber.cs    # For numbers too strange for 16-bit
│   ├── CrazyType.cs               # Message type mappings (like 72 = Hello, 39 = String incoming)
│   ├── CrazyVersion.cs            # Because even versions deserve madness
│   └── CrazyRSA.cs                # RSA public key logic & cursed wrapping
├── Server/                        # The sacred listener of absurdities
└── BSCPTester/                    # The sender of beautifully broken messages
```

---

## 📡 Protocol Flow (BSCP v2)

```
Client -> Hello from client!
Server -> Hello from server, buddy!
Client -> I am at Version 2
Server -> That's great! So am I!
Client -> Here is my RSA public key (wrapped in absurdity)
Server -> Here is mine (ditto)
Client -> Please accept this AES key + IV (encrypted using your RSA key)
Server -> I got it. AES mode engaged.
... All further messages: JSON, AES-encrypted, BSCP-padded, checksummed, and judged.---
```

---

## 🧪 How Checksums Work

Each character:

* Contains:

  * 8-bit ASCII char
  * 2-bit high checksum
  * 2-bit low checksum
  * 1-bit client flag
* Checksum is calculated as:

  ```text
  magic = firstChar ^ lastChar ^ stringLength
  checksum = ((char + magic) >> 4) ^ ((char + magic) & 0xF)
  ```

Each char must prove it belongs to the string.

---

## 🧙 How to Use

1. Build the solution.
2. Run **Server**.
3. Run **Tester**.
4. Watch chaos unfold.

---

## ⚠️ Disclaimer

This protocol:

* Is real.
* Should not be used in production.
* Might be sentient.

You have been warned.

---

## 🩷 Credits

Created by a developer who stared too long into the abyss of Microsoft data types and emerged stronger — and stranger.

Readme made by ChatGPT LOL

Glory to Towa.