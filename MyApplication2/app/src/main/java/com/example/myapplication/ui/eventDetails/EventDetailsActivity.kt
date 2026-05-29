package com.example.myapplication.ui.eventDetails

import android.content.Intent
import android.os.Build
import android.os.Bundle
import android.widget.*
import androidx.annotation.RequiresApi
import androidx.appcompat.app.AppCompatActivity
import com.bumptech.glide.Glide
import com.example.myapplication.R
import com.example.myapplication.data.api.RetrofitClient
import com.example.myapplication.data.api.TokenManager
import com.example.myapplication.data.model.BuyTicketRequest
import com.example.myapplication.ui.purchase.PurchaseActivity
import com.example.myapplication.utils.FavoritesManager
import kotlinx.coroutines.*
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter

class EventDetailsActivity : AppCompatActivity() {

    companion object {
        const val BASE_URL = "http://10.0.2.2:5185"
    }

    @RequiresApi(Build.VERSION_CODES.O)
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_event_details)

        val id = intent.getIntExtra("eventId", 0)
        val token = TokenManager(this).getToken()
        val api = RetrofitClient.create(token)

        // Инициализация Views
        val image = findViewById<ImageView>(R.id.eventImage)
        val title = findViewById<TextView>(R.id.title)
        val description = findViewById<TextView>(R.id.description)
        val date = findViewById<TextView>(R.id.date)
        val location = findViewById<TextView>(R.id.location)
        val organizer = findViewById<TextView>(R.id.organizer)
        val ticketsLeft = findViewById<TextView>(R.id.ticketsLeft)
        val status = findViewById<TextView>(R.id.status)
        val buyBtn = findViewById<Button>(R.id.buyTicket)
        val favBtn = findViewById<ImageButton>(R.id.btnFavorite)

        val favManager = FavoritesManager(this)

        CoroutineScope(Dispatchers.IO).launch {

            val event = api.getEvent(id)

            withContext(Dispatchers.Main) {

                fun updateFavIcon() {
                    val icon =
                        if (favManager.isFavorite(event.id))
                            android.R.drawable.btn_star_big_on
                        else
                            android.R.drawable.btn_star_big_off

                    favBtn.setImageResource(icon)
                }

                updateFavIcon()

                favBtn.setOnClickListener {
                    favManager.toggle(event.id)
                    updateFavIcon()
                }

                title.text = event.title

                description.text = event.description

                location.text = "📍 ${event.location}"

                organizer.text = "Организатор: ${event.organizer}"

                ticketsLeft.text =
                    "Осталось билетов: ${event.ticketsLeft}"

                date.text =
                    com.example.myapplication.utils.DateUtils
                        .formatDate(event.startDate)

                // статус
                when {

                    event.ticketsLeft == 0 -> {

                        status.text = "Sold Out"
                        buyBtn.isEnabled = false
                    }

                    else -> {

                        status.text = "Продажа открыта"
                    }
                }

                val imageUrl =
                    event.imageUrl?.let { BASE_URL + it }

                Glide.with(this@EventDetailsActivity)

                    .load(imageUrl)

                    .placeholder(R.drawable.placeholder_event)

                    .into(image)

                // кнопка покупки
                buyBtn.setOnClickListener {
                    val intent =
                        Intent(this@EventDetailsActivity, PurchaseActivity::class.java).apply {
                            putExtra("eventId", event.id)
                            putExtra("price", event.price)
                            putExtra("title", event.title)
                            putExtra("date", event.startDate)        // <-- дата
                            putExtra("location", event.location)
                        }
                    startActivity(intent)
                }
            }
        }
    }
}

