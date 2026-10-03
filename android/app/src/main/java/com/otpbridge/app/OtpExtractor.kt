package com.otpbridge.app

import kotlin.math.abs

/** Pulls a one-time code out of an SMS body, or returns null if the message isn't an OTP. */
object OtpExtractor {
    private val keyword = Regex(
        "(?i)otp|one[ -]?time|passcode|password|verification|verify|\\bcode\\b|\\bpin\\b|ओटीपी|कोड"
    )
    private val candidate = Regex("(?<![A-Za-z0-9])\\d{4,8}(?![A-Za-z0-9])")
    private val currencyBefore = Regex("(?i)(\\brs\\.?|\\binr|₹|\\$)\\s*$")

    fun extract(text: String): String? {
        val keywords = keyword.findAll(text).map { it.range.first }.toList()
        if (keywords.isEmpty()) return null
        return candidate.findAll(text)
            .filterNot { looksLikeNonOtp(text, it.range) }
            .minByOrNull { m -> keywords.minOf { k -> abs(k - m.range.first) } }
            ?.value
    }

    /** Amounts (Rs 5000, 5000.00), masked accounts (*1234), dates and times (03-10-2026). */
    private fun looksLikeNonOtp(text: String, range: IntRange): Boolean {
        if (currencyBefore.containsMatchIn(text.substring(maxOf(0, range.first - 6), range.first))) return true
        val prev = text.getOrNull(range.first - 1)
        val prev2 = text.getOrNull(range.first - 2)
        val next = text.getOrNull(range.last + 1)
        val next2 = text.getOrNull(range.last + 2)
        if (prev != null && prev in "*.,") return true
        if (prev != null && prev in "-/:" && prev2?.isDigit() == true) return true
        if (next != null && next in ".,-/:" && next2?.isDigit() == true) return true
        return false
    }
}
