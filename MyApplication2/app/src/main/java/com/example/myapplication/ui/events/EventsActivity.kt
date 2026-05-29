package com.example.myapplication.ui.events

import android.content.Intent
import android.os.Bundle
import android.text.Editable
import android.text.TextWatcher
import android.view.MenuItem
import android.view.View
import android.widget.*
import androidx.appcompat.app.ActionBarDrawerToggle
import androidx.appcompat.app.AppCompatActivity
import androidx.appcompat.widget.Toolbar
import androidx.drawerlayout.widget.DrawerLayout
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout
import com.example.myapplication.R
import com.example.myapplication.data.api.RetrofitClient
import com.example.myapplication.data.api.TokenManager
import com.example.myapplication.data.model.Event
import com.example.myapplication.data.model.FilterRequest
import com.example.myapplication.ui.eventDetails.EventDetailsActivity
import com.example.myapplication.ui.profile.ProfileActivity
import com.example.myapplication.ui.ticket.MyTicketsActivity
import com.example.myapplication.utils.FavoritesManager
import com.google.android.material.bottomsheet.BottomSheetDialog
import com.google.android.material.navigation.NavigationView
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.text.SimpleDateFormat
import java.util.*

class EventsActivity : AppCompatActivity() {

    private lateinit var drawerLayout: DrawerLayout
    private lateinit var navigationView: NavigationView
    private lateinit var swipeRefresh: SwipeRefreshLayout
    private lateinit var recyclerView: RecyclerView
    private lateinit var search: EditText
    private lateinit var emptyState: View
    private lateinit var toolbar: Toolbar

    private var adapter: EventsAdapter? = null
    private var allEvents: List<Event> = emptyList()
    private var lastFilter: FilterRequest? = null
    private var showingFavorites = false
    private var currentCategory: String? = null   // сохраняем текущую категорию

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_events)

        drawerLayout = findViewById(R.id.drawerLayout)
        navigationView = findViewById(R.id.navigationView)
        swipeRefresh = findViewById(R.id.swipeRefreshLayout)
        recyclerView = findViewById(R.id.recyclerView)
        search = findViewById(R.id.etSearch)
        emptyState = findViewById(R.id.emptyState)
        toolbar = findViewById(R.id.toolbar)

        recyclerView.layoutManager = LinearLayoutManager(this)

        val category = intent.getStringExtra("category")
        val showFavorites = intent.getBooleanExtra("show_favorites", false)

        setupDrawerMenu()
        loadUserHeader()

        if (showFavorites) {
            showFavorites()
        } else if (!category.isNullOrBlank()) {
            currentCategory = category
            toolbar.title = category
            loadByCategory(category)
        } else {
            currentCategory = null
            toolbar.title = "Мероприятия"
            loadEvents()
        }

        swipeRefresh.setOnRefreshListener {
            refreshCurrentContent()
        }


        search.addTextChangedListener(object : TextWatcher {
            override fun afterTextChanged(s: Editable?) = filterEventsLocally(s.toString())
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) {}
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) {}
        })

        findViewById<ImageButton>(R.id.btnFilter).setOnClickListener { openFilter() }
    }

    private fun setupDrawerMenu() {
        val toggle = ActionBarDrawerToggle(this, drawerLayout, toolbar,
            R.string.open_drawer, R.string.close_drawer)
        drawerLayout.addDrawerListener(toggle)
        toggle.syncState()

        navigationView.setNavigationItemSelectedListener { menuItem ->
            handleMenuClick(menuItem)
            true
        }
    }

    private fun handleMenuClick(item: MenuItem) {
        drawerLayout.closeDrawers()
        lastFilter = null
        showingFavorites = false

        when (item.itemId) {
            R.id.nav_all_events -> {
                currentCategory = null
                toolbar.title = "Мероприятия"
                loadEvents()
            }
            R.id.nav_concert -> openCategory("Концерт")
            R.id.nav_exhibition -> openCategory("Выставка")
            R.id.nav_conference -> openCategory("Конференция")
            R.id.nav_workshop -> openCategory("Воркшоп")
            R.id.nav_my_tickets -> {
                startActivity(Intent(this, MyTicketsActivity::class.java))
                finish()
            }
            R.id.nav_favorites -> showFavorites()
            R.id.nav_profile -> {
                startActivity(Intent(this, ProfileActivity::class.java))
            }
            else -> Toast.makeText(this, "Раздел скоро будет доступен", Toast.LENGTH_SHORT).show()
        }
    }

    private fun openCategory(category: String) {
        currentCategory = category
        toolbar.title = category
        loadByCategory(category)
    }

    private fun loadEvents() = loadFilteredEvents(null)

    private fun loadByCategory(category: String) {
        loadFilteredEvents(FilterRequest(category = category))
    }

    private fun loadFilteredEvents(filter: FilterRequest?) {
        swipeRefresh.isRefreshing = true

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val token = TokenManager(this@EventsActivity).getToken()
                val api = RetrofitClient.create(token)
                var events = if (filter == null) api.getEvents() else api.filterEvents(filter)

                // если сейчас показываем избранное — фильтруем по id избранного
                if (showingFavorites) {
                    val favIds = FavoritesManager(this@EventsActivity).getAll()
                    events = events.filter { favIds.contains(it.id) }
                }

                withContext(Dispatchers.Main) {
                    swipeRefresh.isRefreshing = false
                    allEvents = events

                    if (adapter == null) {
                        adapter = EventsAdapter(events) { event ->
                            val intent = Intent(this@EventsActivity, EventDetailsActivity::class.java)
                            intent.putExtra("eventId", event.id)
                            startActivity(intent)
                        }
                        recyclerView.adapter = adapter
                    } else {
                        adapter?.updateData(events)
                    }

                    updateScreenState(events.isEmpty())
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    swipeRefresh.isRefreshing = false
                    Toast.makeText(this@EventsActivity, "Ошибка загрузки", Toast.LENGTH_LONG).show()
                    updateScreenState(true)
                }
            }
        }
    }

    private fun updateScreenState(isEmpty: Boolean) {
        runOnUiThread {
            if (isEmpty) {
                emptyState.visibility = View.VISIBLE
                recyclerView.visibility = View.GONE
            } else {
                emptyState.visibility = View.GONE
                recyclerView.visibility = View.VISIBLE
            }

            // Принудительное обновление
            recyclerView.requestLayout()
            recyclerView.invalidate()
            emptyState.requestLayout()
        }
    }

    private fun filterEventsLocally(query: String) {
        val baseList = if (showingFavorites) {
            val favIds = FavoritesManager(this).getAll()
            allEvents.filter { favIds.contains(it.id) }
        } else {
            allEvents
        }

        if (query.isBlank()) {
            adapter?.updateData(baseList)
            updateScreenState(baseList.isEmpty())
            return
        }

        val filtered = baseList.filter {
            it.title.contains(query, ignoreCase = true) ||
                    it.location.contains(query, ignoreCase = true) ||
                    it.category.contains(query, ignoreCase = true)
        }

        adapter?.updateData(filtered)
        updateScreenState(filtered.isEmpty())
    }

    private fun openFilter() {
        val dialog = BottomSheetDialog(this)
        val view = layoutInflater.inflate(R.layout.layout_filter, null)

        val categorySpinner = view.findViewById<Spinner>(R.id.filterCategory)
        val dateFrom = view.findViewById<EditText>(R.id.filterDateFrom)
        val dateTo = view.findViewById<EditText>(R.id.filterDateTo)
        val priceMin = view.findViewById<EditText>(R.id.filterPriceMin)
        val priceMax = view.findViewById<EditText>(R.id.filterPriceMax)
        val onlyFree = view.findViewById<CheckBox>(R.id.filterFree)
        val apply = view.findViewById<Button>(R.id.applyFilter)
        val reset = view.findViewById<Button>(R.id.resetFilter)

        val categories = mutableListOf("Все категории")
        categories.addAll(allEvents.map { it.category }.distinct())

        val adapterSpinner = ArrayAdapter(this, android.R.layout.simple_spinner_item, categories)
        adapterSpinner.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item)
        categorySpinner.adapter = adapterSpinner
        categorySpinner.setSelection(0)

        lastFilter?.let { last ->
            val catIndex = categories.indexOf(last.category)
            categorySpinner.setSelection(if (catIndex >= 0) catIndex else 0)
            dateFrom.setText(last.dateFrom ?: "")
            dateTo.setText(last.dateTo ?: "")
            priceMin.setText(last.priceMin?.toString() ?: "")
            priceMax.setText(last.priceMax?.toString() ?: "")
            onlyFree.isChecked = last.onlyFree ?: false
        }

        val calendar = Calendar.getInstance()
        val dateFormat = SimpleDateFormat("yyyy-MM-dd", Locale.getDefault())

        dateFrom.setOnClickListener { showDatePicker(dateFrom, calendar) }
        dateTo.setOnClickListener { showDatePicker(dateTo, calendar) }

        apply.setOnClickListener {
            val selectedCategory = categorySpinner.selectedItem as String
            val request = FilterRequest(
                search = search.text.toString(),
                category = if (selectedCategory == "Все категории") null else selectedCategory,
                dateFrom = dateFrom.text.toString().ifBlank { null },
                dateTo = dateTo.text.toString().ifBlank { null },
                priceMin = priceMin.text.toString().toDoubleOrNull(),
                priceMax = priceMax.text.toString().toDoubleOrNull(),
                onlyFree = onlyFree.isChecked
            )
            lastFilter = request
            loadFilteredEvents(request)
            dialog.dismiss()
        }

        reset.setOnClickListener {
            lastFilter = null
            loadEvents()
            dialog.dismiss()
        }

        dialog.setContentView(view)
        dialog.show()
    }

    private fun showDatePicker(editText: EditText, calendar: Calendar) {
        android.app.DatePickerDialog(this, { _, year, month, day ->
            val selected = Calendar.getInstance()
            selected.set(year, month, day)
            editText.setText(SimpleDateFormat("yyyy-MM-dd", Locale.getDefault()).format(selected.time))
        }, calendar.get(Calendar.YEAR), calendar.get(Calendar.MONTH), calendar.get(Calendar.DAY_OF_MONTH)).show()
    }
    private fun showFavorites() {
        showingFavorites = true
        val favManager = FavoritesManager(this)
        val favIds = favManager.getAll()

        if (favIds.isEmpty()) {
            allEvents = emptyList()
            adapter?.updateData(emptyList())
            updateScreenState(true)
            Toast.makeText(this, "Нет избранных мероприятий", Toast.LENGTH_SHORT).show()
            return
        }

        if (allEvents.isEmpty()) {
            loadAllAndShowFavorites()
        } else {
            val favEvents = allEvents.filter { favIds.contains(it.id) }
            if (adapter == null) {
                adapter = EventsAdapter(favEvents) { event ->
                    val intent = Intent(this@EventsActivity, EventDetailsActivity::class.java)
                    intent.putExtra("eventId", event.id)
                    startActivity(intent)
                }
                recyclerView.adapter = adapter
            } else {
                adapter?.updateData(favEvents)
            }
            updateScreenState(favEvents.isEmpty())
        }
    }
    private fun loadAllAndShowFavorites() {
        swipeRefresh.isRefreshing = true

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val token = TokenManager(this@EventsActivity).getToken()
                val api = RetrofitClient.create(token)
                allEvents = api.getEvents()   // загружаем ВСЕ события

                withContext(Dispatchers.Main) {
                    val favIds = FavoritesManager(this@EventsActivity).getAll()
                    val favEvents = allEvents.filter { favIds.contains(it.id) }

                    if (adapter == null) {
                        adapter = EventsAdapter(favEvents) { event ->
                            val intent = Intent(this@EventsActivity, EventDetailsActivity::class.java)
                            intent.putExtra("eventId", event.id)
                            startActivity(intent)
                        }
                        recyclerView.adapter = adapter
                    } else {
                        adapter?.updateData(favEvents)
                    }

                    updateScreenState(favEvents.isEmpty())
                    swipeRefresh.isRefreshing = false

                    if (favEvents.isEmpty()) {
                        Toast.makeText(this@EventsActivity, "Нет избранных мероприятий", Toast.LENGTH_SHORT).show()
                    }
                }
            } catch (e: Exception) {
                withContext(Dispatchers.Main) {
                    swipeRefresh.isRefreshing = false
                    updateScreenState(true)
                    Toast.makeText(this@EventsActivity, "Ошибка загрузки избранного", Toast.LENGTH_LONG).show()
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
    private fun refreshCurrentContent() {
        if (showingFavorites) {
            showFavorites()
        } else if (currentCategory != null) {
            loadByCategory(currentCategory!!)
        } else {
            loadEvents()
        }
    }
}
