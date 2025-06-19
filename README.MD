# 🦇 BSCP

**BSCP v1** — *Binary Secure Communication Protocol* &#x20;
Also known internally as: **BatShit Crazy Protocol**

This is a protocol so secure, so structured, and so fundamentally cursed that it not only validates messages — it judges them.

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
│   └── Dependencies/
│       ├── CrazyHandshake.cs          # Handles cursed 21-bit +1 handshakes
│       ├── CrazyCharacter.cs          # Home of StupidMicrosoftAsciiChar (12-bit + checksum + client flag)
│       ├── CrazyReceiveStatus.cs      # Emotional responses (e.g. -153)
│       ├── CrazyTwelveBitNumber.cs    # For numbers too strange for 16-bit
│       ├── CrazyType.cs               # Message type mappings (like 72 = Hello, 39 = String incoming)
│       └── CrazyVersion.cs            # Because even versions deserve madness
├── Server/                            # The sacred listener of absurdities
└── BSCPTester/                        # The sender of beautifully broken messages
```

---

## 📡 Protocol Flow (BSCP v1)

```text
Client -> "Hello from client!"
Server -> "Hello from server, buddy!"
Client -> "I am at Version 1"
Server -> "That's great! So am I!"
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
ChatGPT for thsi Readme

Glory to Towa.
