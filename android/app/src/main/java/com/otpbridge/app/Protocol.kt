package com.otpbridge.app

import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress
import java.net.InetSocketAddress
import java.net.Socket
import java.security.MessageDigest
import java.security.SecureRandom
import java.util.Base64
import javax.crypto.Cipher
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.SecretKeySpec

/** What the phone learns from the PC's QR code: `OTPB1|name|ip1,ip2|port|base64key`. */
data class PairInfo(val name: String, val hosts: List<String>, val port: Int, val key: ByteArray) {
    fun encode(): String =
        listOf("OTPB1", name, hosts.joinToString(","), port.toString(), Base64.getEncoder().encodeToString(key))
            .joinToString("|")

    companion object {
        fun decode(code: String): PairInfo? {
            val parts = code.trim().split("|")
            if (parts.size != 5 || parts[0] != "OTPB1") return null
            val key = runCatching { Base64.getDecoder().decode(parts[4]) }.getOrNull() ?: return null
            if (key.size != 32) return null
            val port = parts[3].toIntOrNull() ?: return null
            return PairInfo(parts[1], parts[2].split(",").filter { it.isNotBlank() }, port, key)
        }
    }
}

/**
 * Phone -> PC wire protocol. Plain JVM code (no Android APIs) so it can be unit-tested
 * against the real Windows server.
 *
 * TCP: one line of base64(nonce[12] + AES-256-GCM ciphertext+tag), PC answers "OK" or "ERR".
 * UDP discovery (when the PC's IP changed): broadcast "OTPBRIDGE_DISCOVER <keyId>",
 * the paired PC answers "OTPBRIDGE_HERE <keyId> <port>".
 */
object Protocol {
    const val DISCOVERY_PORT = 47322
    private const val TIMEOUT_MS = 1500
    private val random = SecureRandom()

    fun keyId(key: ByteArray): String =
        MessageDigest.getInstance("SHA-256").digest(key).take(4).joinToString("") { "%02x".format(it) }

    fun buildMessage(type: String, otp: String, from: String, ts: Long = System.currentTimeMillis() / 1000): String =
        """{"type":${json(type)},"otp":${json(otp)},"from":${json(from)},"ts":$ts}"""

    fun encrypt(key: ByteArray, plaintext: String): String {
        val nonce = ByteArray(12).also(random::nextBytes)
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.ENCRYPT_MODE, SecretKeySpec(key, "AES"), GCMParameterSpec(128, nonce))
        return Base64.getEncoder().encodeToString(nonce + cipher.doFinal(plaintext.toByteArray()))
    }

    /** Returns the host that accepted the message, or null if the PC couldn't be reached. */
    fun deliver(pair: PairInfo, message: String, broadcasts: List<InetAddress> = emptyList()): String? {
        pair.hosts.firstOrNull { trySend(it, pair.port, encrypt(pair.key, message)) }?.let { return it }
        val found = discover(pair, broadcasts) ?: return null
        return found.takeIf { trySend(it, pair.port, encrypt(pair.key, message)) }
    }

    private fun trySend(host: String, port: Int, line: String): Boolean = try {
        Socket().use { s ->
            s.connect(InetSocketAddress(host, port), TIMEOUT_MS)
            s.soTimeout = TIMEOUT_MS
            s.getOutputStream().apply { write("$line\n".toByteArray()); flush() }
            s.getInputStream().bufferedReader().readLine() == "OK"
        }
    } catch (e: Exception) {
        false
    }

    fun discover(pair: PairInfo, broadcasts: List<InetAddress>): String? {
        try {
            DatagramSocket().use { sock ->
                sock.broadcast = true
                sock.soTimeout = TIMEOUT_MS
                val id = keyId(pair.key)
                val probe = "OTPBRIDGE_DISCOVER $id".toByteArray()
                for (target in (broadcasts + InetAddress.getByName("255.255.255.255")).distinct()) {
                    runCatching { sock.send(DatagramPacket(probe, probe.size, target, DISCOVERY_PORT)) }
                }
                val buf = ByteArray(256)
                while (true) {
                    val packet = DatagramPacket(buf, buf.size)
                    sock.receive(packet) // throws SocketTimeoutException when nobody answers
                    if (String(packet.data, 0, packet.length).startsWith("OTPBRIDGE_HERE $id ")) {
                        return packet.address.hostAddress
                    }
                }
            }
        } catch (e: Exception) {
            return null
        }
    }

    private fun json(s: String) = buildString {
        append('"')
        for (c in s) when {
            c == '"' -> append("\\\"")
            c == '\\' -> append("\\\\")
            c < ' ' -> append("\\u%04x".format(c.code))
            else -> append(c)
        }
        append('"')
    }
}
