using Mapster;
using StreamForge.Domain.Entities;
using StreamForge.Infrastructure.Persistence.DynamoDb;

namespace StreamForge.Infrastructure.Mappers;

public static class MapsterConfig
{
    public static void Configure()
    {
        // Configuração Global do Mapster
        TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true);

        // Video -> VideoDocument
        TypeAdapterConfig<Video, VideoDocument>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FileName, src => src.FileName)
            .Map(dest => dest.OriginalName, src => src.OriginalName);

        // VideoDocument -> Video
        // Mapster consegue instanciar classes com construtor se os parametros baterem
        // ou podemos usar .ConstructUsing
        TypeAdapterConfig<VideoDocument, Video>
            .NewConfig()
            .ConstructUsing(doc => new Video(doc.FileName!, doc.OriginalName!)) // Usa construtor público
            .AfterMapping((src, dest) => 
            {
                // Preenche as propriedades que não estão no construtor via métodos Set
                dest.SetId(src.Id);
                if (src.FileSize.HasValue) dest.SetFileSize(src.FileSize.Value);
                dest.SetStatus(src.Status);
                dest.SetCreatedAt(src.CreatedAt);
                dest.SetProcessedAt(src.ProcessedAt);
                dest.SetS3Key(src.S3Key!);
                dest.SetDuration(src.Duration);
                dest.SetFormat(src.Format);
            });

        // User -> UserDocument
        TypeAdapterConfig<User, UserDocument>.NewConfig();

        // UserDocument -> User
        TypeAdapterConfig<UserDocument, User>
            .NewConfig()
            .ConstructUsing(doc => new User(doc.Email!, doc.PasswordHash!))
            .AfterMapping((src, dest) =>
            {
                dest.SetId(src.Id);
                dest.SetCreatedAt(src.CreatedAt);
            });
    }
}
