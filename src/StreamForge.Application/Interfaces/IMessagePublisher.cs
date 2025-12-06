namespace StreamForge.Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string topicName, T message);
}
