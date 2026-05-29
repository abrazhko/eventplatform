package com.example.myapplication.ui.ticket

import android.os.Build
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.annotation.RequiresApi
import androidx.recyclerview.widget.RecyclerView
import com.example.myapplication.R
import com.example.myapplication.data.model.Ticket
import com.example.myapplication.utils.DateUtils
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter

class TicketAdapter(
    private val tickets: List<Ticket>,
    private val onClick: (Ticket) -> Unit
) : RecyclerView.Adapter<TicketAdapter.Holder>() {

    class Holder(view: View) : RecyclerView.ViewHolder(view) {
        val title: TextView = view.findViewById(R.id.tvTitle)
        val date: TextView = view.findViewById(R.id.tvDate)
        val status: TextView = view.findViewById(R.id.tvStatus)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Holder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_ticket, parent, false)
        return Holder(view)
    }

    override fun getItemCount() = tickets.size

    @RequiresApi(Build.VERSION_CODES.O)
    override fun onBindViewHolder(holder: Holder, position: Int) {
        val t = tickets[position]
        holder.title.text = t.eventTitle

        val purchaseDateStr = t.purchasedAt ?: t.startDate
        val formattedDate =
            DateUtils.formatDate(purchaseDateStr)

        holder.date.text =
            DateUtils.formatDate(purchaseDateStr)

        holder.status.text = when (t.status) {
            1 -> "🟢 Активен"
            2 -> "⚪ Использован"
            else -> "Отменён"
        }
        holder.status.setTextColor(
            if (t.status == 1) 0xFF4CAF50.toInt() else 0xFF666666.toInt()
        )

        holder.itemView.setOnClickListener { onClick(t) }
    }
}