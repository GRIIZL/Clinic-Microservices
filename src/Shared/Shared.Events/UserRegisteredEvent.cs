namespace Shared.Events
{
    /// <summary>
    /// Событие, публикуемое сервисом Auth после успешной регистрации пользователя.
    /// Другие микросервисы (например, Profiles) подписываются на него,
    /// чтобы синхронизировать собственные данные (создание/связывание профиля).
    /// </summary>
    public class UserRegisteredEvent
    {
        /// <summary>Идентификатор созданного аккаунта (Account.Id из Auth DB).</summary>
        public Guid UserId { get; set; }

        /// <summary>Email пользователя (нормализован в нижний регистр).</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Телефон пользователя (необязательное поле).</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>Роль пользователя: "Patient" (для нынешнего сценария).</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Момент создания аккаунта (UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}