package com.example.myapplication.ui.purchase

import android.content.Intent
import android.os.Build
import android.os.Bundle
import android.widget.Button
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.example.myapplication.R
import com.example.myapplication.data.api.RetrofitClient
import com.example.myapplication.data.api.TokenManager
import com.example.myapplication.data.model.BuyTicketRequest
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class PurchaseActivity : AppCompatActivity() {

    private var quantity = 1
    private var pricePerTicket = 0.0
    private var eventId = 0
    private var eventTitle = ""
    private var eventDate = ""
    private var eventLocation = ""

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_purchase)

        eventId = intent.getIntExtra("eventId", 0)
        pricePerTicket = intent.getDoubleExtra("price", 0.0)
        eventTitle = intent.getStringExtra("title") ?: "Мероприятие"
        eventDate = intent.getStringExtra("date") ?: ""
        eventLocation = intent.getStringExtra("location") ?: ""

        val tvTitle = findViewById<TextView>(R.id.tvEventTitle)
        val tvDate = findViewById<TextView>(R.id.tvEventDate)
        val tvQuantity = findViewById<TextView>(R.id.tvQuantity)
        val tvTotal = findViewById<TextView>(R.id.tvTotalPrice)
        val btnMinus = findViewById<Button>(R.id.btnMinus)
        val btnPlus = findViewById<Button>(R.id.btnPlus)
        val btnPay = findViewById<Button>(R.id.btnPay)

        tvTitle.text = eventTitle
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            tvDate.text = com.example.myapplication.utils.DateUtils.formatDate(eventDate)
        } else {
            tvDate.text = eventDate
        }

        updateTotal(tvTotal, tvQuantity)

        btnMinus.setOnClickListener {
            if (quantity > 1) {
                quantity--
                updateTotal(tvTotal, tvQuantity)
            }
        }

        btnPlus.setOnClickListener {
            quantity++
            updateTotal(tvTotal, tvQuantity)
        }

        btnPay.setOnClickListener {
            startPaymentProcessing()
        }
    }

    private fun updateTotal(tvTotal: TextView, tvQuantity: TextView) {
        tvQuantity.text = quantity.toString()
        val total = quantity * pricePerTicket
        tvTotal.text = "${total.toInt()} ₽"
    }

    private fun startPaymentProcessing() {
        val token = TokenManager(this).getToken()
        if (token == null) {
            Toast.makeText(this, "Необходимо авторизоваться", Toast.LENGTH_SHORT).show()
            return
        }

        val api = RetrofitClient.create(token)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                var lastTicketId = 0
                var lastQrCode = ""

                repeat(quantity) {
                    val ticket = api.buyTicket(BuyTicketRequest(eventId))
                    lastTicketId = ticket.id
                    lastQrCode = ticket.qrCode
                }

                withContext(Dispatchers.Main) {
                    val intent = Intent(this@PurchaseActivity, PaymentProcessingActivity::class.java).apply {
                        putExtra("ticketId", lastTicketId)
                        putExtra("title", eventTitle)
                        putExtra("date", eventDate)
                        putExtra("location", eventLocation)
                        putExtra("qrCode", lastQrCode)
                    }
                    startActivity(intent)
                    finish()
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@PurchaseActivity, "Ошибка покупки: ${e.message}", Toast.LENGTH_LONG).show()
                }
            }
        }
    }
}