package com.otpbridge.app

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assume.assumeTrue
import org.junit.Test

class ProtocolTest {
    @Test fun pairCodeRoundTrip() {
        val p = PairInfo("DESKTOP-1", listOf("192.168.1.5", "10.0.0.2"), 47321, ByteArray(32) { it.toByte() })
        val back = PairInfo.decode(p.encode())!!
        assertEquals(p.hosts, back.hosts)
        assertEquals(p.port, back.port)
        assertEquals(p.key.toList(), back.key.toList())
    }

    @Test fun rejectsBadPairCode() {
        assertNull(PairInfo.decode("hello"))
        assertNull(PairInfo.decode("OTPB1|pc|1.2.3.4|47321|c2hvcnQ="))
    }

    // Same vector is checked on the Windows side: SHA-256 of 32 zero bytes starts with 66687aad.
    @Test fun keyIdMatchesWindows() = assertEquals("66687aad", Protocol.keyId(ByteArray(32)))

    @Test fun jsonEscaping() = assertEquals(
        """{"type":"otp","otp":"1234","from":"a\"b\\c","ts":5}""",
        Protocol.buildMessage("otp", "1234", "a\"b\\c", 5)
    )

    /** Sends to a running Windows Harness. Run with OTPB_E2E_CODE=<PAIRCODE printed by harness>. */
    @Test fun endToEndWithWindowsServer() {
        val code = System.getenv("OTPB_E2E_CODE")
        assumeTrue(code != null)
        val pair = PairInfo.decode(code!!)!!
        assertNotNull(Protocol.deliver(pair, Protocol.buildMessage("test", "654321", "e2e")))
        // Wrong host first: falls back to UDP discovery and still delivers.
        assertNotNull(Protocol.deliver(pair.copy(hosts = listOf("10.255.255.1")), Protocol.buildMessage("otp", "112233", "discovery")))
    }
}
