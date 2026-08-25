namespace Services.Application.Interfaces
{
    public interface IMessageBusClient
    {
        Task PublishSpecializationStatusChanged(string specializationId, string newStatus);
    }
}
