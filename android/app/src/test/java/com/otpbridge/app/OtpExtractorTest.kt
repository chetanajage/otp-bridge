package com.otpbridge.app

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test

class OtpExtractorTest {
    private fun otp(s: String) = OtpExtractor.extract(s)

    @Test fun plainOtp() = assertEquals("482913", otp("482913 is your OTP for login to XYZ. Do not share it with anyone."))
    @Test fun skipsAmount() = assertEquals("739201", otp("Your OTP for txn of Rs 5000.00 at AMAZON is 739201. Valid for 10 mins."))
    @Test fun skipsAmountNoSpace() = assertEquals("118822", otp("OTP 118822 for payment of Rs.2500 to Swiggy"))
    @Test fun fourDigit() = assertEquals("4821", otp("Dear Customer, use 4821 as verification code."))
    @Test fun googleStyle() = assertEquals("582014", otp("G-582014 is your Google verification code."))
    @Test fun hindi() = assertEquals("556677", otp("आपका ओटीपी 556677 है"))
    @Test fun skipsDate() = assertEquals("302211", otp("Use OTP 302211 to login. Valid till 03-10-2026."))
    @Test fun bankDebitIsNotOtp() = assertNull(otp("Your a/c XX1234 debited by INR 2,500 on 03-10-26. Avl bal 10230"))
    @Test fun promoCodeIsNotOtp() = assertNull(otp("Use code SAVE50 to get 50% off!"))
    @Test fun maskedCard() = assertEquals("990011", otp("OTP for card *4455 is 990011"))
    @Test fun hoursIsNotCurrency() = assertEquals("7788", otp("Your code is valid for 2 hours 7788"))
}
