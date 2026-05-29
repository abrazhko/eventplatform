package com.example.myapplication.ui.purchase

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.example.myapplication.R
import com.example.myapplication.ui.ticket.TicketDetailActivity
import kotlinx.coroutines.*

class PaymentProcessingActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_payment_processing)

        val ticketId = intent.getIntExtra("ticketId", 0)
        val title = intent.getStringExtra("title") ?: ""
        val date = intent.getStringExtra("date") ?: ""
        val location = intent.getStringExtra("location") ?: ""
        val qrCode = intent.getStringExtra("qrCode") ?: ""

        // Имитация обработки платежа
        CoroutineScope(Dispatchers.Main).launch {
            delay(2800) // 2.8 секунды

            val intent = Intent(this@PaymentProcessingActivity, TicketDetailActivity::class.java).apply {
                putExtra("ticketId", ticketId)
                putExtra("title", title)
                putExtra("date", date)
                putExtra("location", location)
                putExtra("qrCode", qrCode)
            }
            startActivity(intent)
            finish()
        }
    }
}