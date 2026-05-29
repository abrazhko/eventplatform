package com.example.myapplication.data.api

import com.example.myapplication.data.model.*
import retrofit2.http.*

interface ApiService {

    @POST("auth/login")
    suspend fun login(
        @Body request: LoginRequest
    ): LoginResponse

    @POST("auth/register")
    suspend fun register(
        @Body request: RegisterRequest
    )

    @GET("events")
    suspend fun getEvents(): List<Event>

    @POST("tickets/buy")
    suspend fun buyTicket(
        @Body request: BuyTicketRequest
    ): Ticket

    @GET("tickets/my")
    suspend fun myTickets(): List<Ticket>

    @POST("events/filter")
    suspend fun filterEvents(
        @Body request: FilterRequest
    ): List<Event>

    @GET("events/{id}")
    suspend fun getEvent(
        @Path("id") id: Int
    ): EventDetails

    @GET("profile")
    suspend fun getProfile(): ProfileResponse

    @PUT("profile")
    suspend fun updateProfile(@Body request: UpdateProfileRequest): ProfileResponse
}