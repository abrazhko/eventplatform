package com.example.myapplication.data.model

data class EventDetails(
    val id: Int,
    val title: String,
    val description: String,
    val startDate: String,
    val endDate: String?,
    val location: String,
    val price: Double,
    val category: String,
    val organizer: String,
    val ticketsLeft: Int,
    val imageUrl: String?
)