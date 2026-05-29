package com.example.myapplication.ui.ticket

import android.content.Intent
import android.os.Bundle
import android.view.MenuItem
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.ActionBarDrawerToggle
import androidx.appcompat.app.AppCompatActivity
import androidx.drawerlayout.widget.DrawerLayout
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.example.myapplication.R
import com.example.myapplication.data.api.RetrofitClient
import com.example.myapplication.data.api.TokenManager
import com.example.myapplication.ui.events.EventsActivity
import com.example.myapplication.ui.profile.ProfileActivity
import com.google.android.material.navigation.NavigationView
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class MyTicketsActivity : AppCompatActivity() {

    private lateinit var drawerLayout: DrawerLayout
    private lateinit var navigationView: NavigationView
    private lateinit var recyclerView: RecyclerView
    private lateinit var swipeRefresh: SwipeRefreshLayout

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_my_tickets)

        drawerLayout = findViewById(R.id.drawerLayout)
        navigationView = findViewById(R.id.navigationView)
        recyclerView = findViewById(R.id.recyclerView)
        swipeRefresh = findViewById(R.id.swipeRefresh)

        recyclerView.layoutManager = LinearLayoutManager(this)

        setupDrawerMenu()
        loadUserHeader()
        loadMyTickets()

        swipeRefresh.setOnRefreshListener { loadMyTickets() }
    }

    private fun setupDrawerMenu() {
        val toggle = ActionBarDrawerToggle(
            this,
            drawerLayout,
            findViewById(R.id.toolbar),
            R.string.open_drawer,
            R.string.close_drawer
        )
        drawerLayout.addDrawerListener(toggle)
        toggle.syncState()

        navigationView.setNavigationItemSelectedListener { menuItem ->
            handleMenuClick(menuItem)
            true
        }
    }

    private fun handleMenuClick(item: MenuItem) {
        drawerLayout.closeDrawers()

        when (item.itemId) {
            R.id.nav_all_events -> {
                startActivity(Intent(this, EventsActivity::class.java))
                finish()
            }

            R.id.nav_concert -> openCategory("Концерт")
            R.id.nav_exhibition -> openCategory("Выставка")
            R.id.nav_conference -> openCategory("Конференция")
            R.id.nav_workshop -> openCategory("Воркшоп")

            R.id.nav_my_tickets -> {
                // уже здесь мы
            }
            R.id.nav_favorites -> {
                startActivity(Intent(this, EventsActivity::class.java).apply {
                    putExtra("show_favorites", true)
                })
                finish()
            }
            R.id.nav_profile -> {
                startActivity(Intent(this, ProfileActivity::class.java))
            }
            else -> Toast.makeText(this, "Раздел скоро будет доступен", Toast.LENGTH_SHORT).show()
        }
    }

    private fun openCategory(category: String) {
        val intent = Intent(this, EventsActivity::class.java).apply {
            putExtra("category", category)
        }
        startActivity(intent)
        finish() // закрываем текущий экран
    }

    private fun loadMyTickets() {
        swipeRefresh.isRefreshing = true
        val token = TokenManager(this).getToken()
        val api = RetrofitClient.create(token)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                var tickets = api.myTickets()
                tickets = tickets.sortedByDescending { it.purchasedAt ?: it.startDate }

                withContext(Dispatchers.Main) {
                    swipeRefresh.isRefreshing = false

                    val adapter = TicketAdapter(tickets) { ticket ->
                        val intent = Intent(this@MyTicketsActivity, TicketDetailActivity::class.java)
                        intent.putExtra("title", ticket.eventTitle)
                        intent.putExtra("date", ticket.startDate)
                        intent.putExtra("location", ticket.location)
                        intent.putExtra("qrCode", ticket.qrCode)
                        intent.putExtra("status", ticket.status)
                        startActivity(intent)
                    }
                    recyclerView.adapter = adapter
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    swipeRefresh.isRefreshing = false
                    Toast.makeText(this@MyTicketsActivity, "Не удалось загрузить билеты", Toast.LENGTH_LONG).show()
                }
            }
        }
    }
    private fun loadUserHeader() {
        val headerView = navigationView.getHeaderView(0) ?: return
        val tvUserName = headerView.findViewById<TextView>(R.id.tvUserName)
        val tvUserEmail = headerView.findViewById<TextView>(R.id.tvUserEmail)

        val token = TokenManager(this).getToken()
        val api = RetrofitClient.create(token)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val user = api.getProfile()
                withContext(Dispatchers.Main) {
                    tvUserName.text = user.fullName ?: "Пользователь"
                    tvUserEmail.text = user.email ?: ""
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    tvUserName.text = "Пользователь"
                    tvUserEmail.text = ""
                }
            }
        }
    }
}