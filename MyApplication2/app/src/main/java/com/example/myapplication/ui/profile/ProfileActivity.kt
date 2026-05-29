package com.example.myapplication.ui.profile

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import com.example.myapplication.R
import com.example.myapplication.data.api.RetrofitClient
import com.example.myapplication.data.api.TokenManager
import com.example.myapplication.data.model.UpdateProfileRequest
import com.example.myapplication.ui.login.LoginActivity // ← добавь этот импорт
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class ProfileActivity : AppCompatActivity() {

    private lateinit var tvUserName: TextView
    private lateinit var tvUserEmail: TextView
    private lateinit var etName: EditText
    private lateinit var etEmail: EditText
    private lateinit var btnSave: Button
    private lateinit var btnLogout: Button

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_profile)

        tvUserName = findViewById(R.id.tvUserName)
        tvUserEmail = findViewById(R.id.tvUserEmail)
        etName = findViewById(R.id.etName)
        etEmail = findViewById(R.id.etEmail)
        btnSave = findViewById(R.id.btnSave)
        btnLogout = findViewById(R.id.btnLogout)

        loadProfile()

        btnSave.setOnClickListener { saveProfile() }
        btnLogout.setOnClickListener { logout() }
    }

    private fun loadProfile() {
        val token = TokenManager(this).getToken()
        val api = RetrofitClient.create(token)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                val user = api.getProfile()
                withContext(Dispatchers.Main) {
                    tvUserName.text = user.fullName
                    tvUserEmail.text = user.email
                    etName.setText(user.fullName)
                    etEmail.setText(user.email)
                }
            } catch (e: Exception) {
                e.printStackTrace()
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@ProfileActivity, "Не удалось загрузить профиль", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    private fun saveProfile() {
        val newName = etName.text.toString().trim()
        val newEmail = etEmail.text.toString().trim()
        if (newName.isEmpty() || newEmail.isEmpty()) {
            Toast.makeText(this, "Имя и email не могут быть пустыми", Toast.LENGTH_SHORT).show()
            return
        }

        val token = TokenManager(this).getToken()
        val api = RetrofitClient.create(token)

        CoroutineScope(Dispatchers.IO).launch {
            try {
                // создаём объект запроса
                val request = UpdateProfileRequest(fullName = newName, email = newEmail)
                val updatedUser = api.updateProfile(request) // передаём объект
                withContext(Dispatchers.Main) {
                    tvUserName.text = updatedUser.fullName
                    tvUserEmail.text = updatedUser.email
                    Toast.makeText(this@ProfileActivity, "Профиль обновлён", Toast.LENGTH_SHORT).show()
                }
            } catch (e: Exception) {
                e.printStackTrace()
                withContext(Dispatchers.Main) {
                    Toast.makeText(this@ProfileActivity, "Не удалось обновить профиль", Toast.LENGTH_SHORT).show()
                }
            }
        }
    }

    private fun logout() {
        TokenManager(this).clearToken() // очищаем токен

        val intent = Intent(this, LoginActivity::class.java)
        intent.flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        startActivity(intent)
        finish() // рекомендуется добавить
    }
}