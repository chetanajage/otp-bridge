package com.otpbridge.app

import android.content.Context
import android.net.ConnectivityManager
import java.net.Inet4Address
import java.net.InetAddress
import java.nio.ByteBuffer
import java.text.DateFormat
import java.util.Date

object PairStore {
    private fun prefs(ctx: Context) = ctx.getSharedPreferences("otpbridge", Context.MODE_PRIVATE)

    fun load(ctx: Context): PairInfo? = prefs(ctx).getString("pair", null)?.let(PairInfo::decode)
    fun save(ctx: Context, pair: PairInfo) = prefs(ctx).edit().putString("pair", pair.encode()).apply()
    fun clear(ctx: Context) = prefs(ctx).edit().remove("pair").apply()
    fun status(ctx: Context): String = prefs(ctx).getString("status", "") ?: ""
    fun setStatus(ctx: Context, status: String) = prefs(ctx).edit().putString("status", status).apply()
}

/** Sends an OTP to the paired PC. Blocking — call off the main thread. */
object Relay {
    fun send(ctx: Context, type: String, otp: String, from: String): Boolean {
        val pair = PairStore.load(ctx) ?: return false.also { PairStore.setStatus(ctx, "Not paired") }
        val host = Protocol.deliver(pair, Protocol.buildMessage(type, otp, from), broadcastAddresses(ctx))
        // Remember where the PC was found so the next OTP goes there first.
        if (host != null && host != pair.hosts.firstOrNull()) {
            PairStore.save(ctx, pair.copy(hosts = listOf(host) + (pair.hosts - host)))
        }
        val time = DateFormat.getTimeInstance(DateFormat.SHORT).format(Date())
        PairStore.setStatus(
            ctx,
            if (host != null) "✓ $otp sent to ${pair.name} ($host) at $time"
            else "✗ Couldn't reach ${pair.name} at $time. Same Wi-Fi? PC app running?"
        )
        return host != null
    }

    private fun broadcastAddresses(ctx: Context): List<InetAddress> {
        val cm = ctx.getSystemService(ConnectivityManager::class.java)
        val props = cm.getLinkProperties(cm.activeNetwork) ?: return emptyList()
        return props.linkAddresses.mapNotNull { la ->
            val addr = la.address as? Inet4Address ?: return@mapNotNull null
            val mask = if (la.prefixLength == 0) 0 else -1 shl (32 - la.prefixLength)
            val ip = ByteBuffer.wrap(addr.address).int
            InetAddress.getByAddress(ByteBuffer.allocate(4).putInt(ip or mask.inv()).array())
        }
    }
}
