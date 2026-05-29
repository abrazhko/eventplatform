package com.example.myapplication.ui.register

import android.content.Intent
import android.os.Bundle
import android.widget.Button
import android.widget.EditText
import androidx.appcompat.app.AppCompatActivity
import com.example.myapplication.R
import com.example.myapplication.data.api.RetrofitClient
import com.example.myapplication.data.api.TokenManager
import com.example.myapplication.data.model.LoginRequest
import com.example.myapplication.data.model.RegisterRequest
import com.example.myapplication.ui.events.EventsActivity
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

class RegisterActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_register)

        val fullNameEdit = findViewById<EditText>(R.id.fullNameEdit)
        val emailEdit = findViewById<EditText>(R.id.emailEdit)
        val passwordEdit = findViewById<EditText>(R.id.passwordEdit)
        val confirmPasswordEdit = findViewById<EditText>(R.id.confirmPasswordEdit)
        val registerBtn = findViewById<Button>(R.id.registerButton)

        val tokenManager = TokenManager(this)
        val api = RetrofitClient.create(null)

        registerBtn.setOnClickListener {
            val fullName = fullNameEdit.text.toString().trim()
            val email = emailEdit.text.toString().trim()
            val password = passwordEdit.text.toString()
            val confirmPassword = confirmPasswordEdit.text.toString()

            // Валидация
            if(fullName.isEmpty()) {
                fullNameEdit.error = "Введите имя"
                return@setOnClickListener
            }

            if(email.isEmpty() || !android.util.Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
                emailEdit.error = "Введите корректный email"
                return@setOnClickListener
            }

            if(password.length < 6) {
                passwordEdit.error = "Пароль минимум 6 символов"
                return@setOnClickListener
            }

            if(password != confirmPassword) {
                confirmPasswordEdit.error = "Пароли не совпадают"
                return@setOnClickListener
            }

            // Создание пользователя
            CoroutineScope(Dispatchers.IO).launch {
                try {
                    // Регистрация
                    api.register(
                        RegisterRequest(
                            fullName,
                            email,
                            password
                        )
                    )

                    val loginResponse = api.login(LoginRequest(email, password))
                    tokenManager.saveToken(loginResponse.token)

                    withContext(Dispatchers.Main) {
                        startActivity(Intent(this@RegisterActivity, EventsActivity::class.java))
                        finish()
                    }

                } catch (e: Exception) {
                    withContext(Dispatchers.Main) {
                        emailEdit.error = "Ошибка регистрации или email уже существует"
                    }
                }
            }
        }
    }
}