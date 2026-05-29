package com.example.myapplication.ui.events

import android.os.Build
import android.util.Log
import android.view.LayoutInflater
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import android.view.View
import android.view.ViewGroup
import android.widget.ImageButton
import android.widget.ImageView
import android.widget.TextView
import androidx.annotation.RequiresApi
import androidx.recyclerview.widget.RecyclerView
import com.bumptech.glide.Glide
import com.example.myapplication.R
import com.example.myapplication.data.model.Event
import com.example.myapplication.utils.FavoritesManager

class EventsAdapter(private var list: List<Event>, private val onClick: (Event) -> Unit) :
    RecyclerView.Adapter<EventsAdapter.Holder>() {

    companion object {
        const val BASE_URL = "http://10.0.2.2:5185"
    }

    class Holder(v: View) : RecyclerView.ViewHolder(v) {
        val image: ImageView = v.findViewById(R.id.eventImage)
        val title: TextView = v.findViewById(R.id.titleText)
        val date: TextView = v.findViewById(R.id.dateText)
        val location: TextView = v.findViewById(R.id.locationText)
        val price: TextView = v.findViewById(R.id.priceText)
        val category: TextView = v.findViewById(R.id.categoryText)

        val favorite: ImageButton = v.findViewById(R.id.btnFavorite)
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Holder {
        val view = LayoutInflater.from(parent.context)
            .inflate(R.layout.item_event, parent, false)
        return Holder(view)
    }

    override fun getItemCount() = list.size

    @RequiresApi(Build.VERSION_CODES.O)
    override fun onBindViewHolder(holder: Holder, position: Int) {
        val e = list[position]

        holder.itemView.setOnClickListener {

            onClick(e)
        }

        holder.title.text = e.title
        holder.date.text =
            com.example.myapplication.utils.DateUtils
                .formatDate(e.startDate)
        holder.location.text = "📍 ${e.location}"
        holder.category.text = e.category

        // Цена
        if (e.price == 0.0) {
            holder.price.text = "Бесплатно"
            holder.price.setTextColor(holder.itemView.context.getColor(R.color.green))
        } else {
            holder.price.text = "${e.price.toInt()} ₽"
            holder.price.setTextColor(holder.itemView.context.getColor(android.R.color.black))
        }

        // Фото
        val imageUrl = e.imageUrl?.let { BASE_URL + it } ?: ""
        if (imageUrl.isNotEmpty()) {
            Glide.with(holder.itemView.context)
                .load(imageUrl)
                .centerCrop()
                .placeholder(R.drawable.placeholder_event)
                .error(R.drawable.error_image)
                .into(holder.image)
        } else {
            holder.image.setImageResource(R.drawable.placeholder_event)
        }
        val favManager = FavoritesManager(holder.itemView.context)

        fun updateIcon() {
            val icon =
                if (favManager.isFavorite(e.id))
                    android.R.drawable.btn_star_big_on
                else
                    android.R.drawable.btn_star_big_off

            holder.favorite.setImageResource(icon)
        }

        updateIcon()

        holder.favorite.setOnClickListener {
            favManager.toggle(e.id)
            updateIcon()
        }
    }

    
    fun updateData(newList: List<Event>) {
        list = newList
        notifyDataSetChanged()
    }
}