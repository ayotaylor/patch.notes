using Backend.Models.Auth;

namespace Backend.Mapping
{
    public static class UserProfileMapper
    {
        public static UserProfile UserToUserProfile(this User user)
        {
            return new UserProfile
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
            };
        }

        // TODO: maybe remove this method if not needed
        public static User UserProfileToUser(this UserProfile userProfile)
        {
            return new User
            {
                Id = userProfile.UserId,
                FirstName = userProfile.FirstName,
                LastName = userProfile.LastName,
                Email = userProfile.Email,
                PhoneNumber = userProfile.PhoneNumber,
                UserProfile = userProfile,
            };
        }
        
        public static UserProfileDto ToDto(this UserProfile userProfile)
        {
            return new UserProfileDto
            {
                FirstName = userProfile.FirstName,
                LastName = userProfile.LastName,
                DisplayName = userProfile.DisplayName,
                Bio = userProfile.Bio,
                ProfileImageUrl = userProfile.ProfileUrlImageUrl,
                DateOfBirth = userProfile.DateOfBirth,
                PhoneNumber = userProfile.PhoneNumber,
                CreatedAt = userProfile.CreatedAt,
                UpdatedAt = userProfile.UpdatedAt,
                Email = userProfile.Email,
                IsProfileUpdated = userProfile.IsProfileUpdated,
            };
        }

        public static UserProfile FromDto(this UserProfileDto userProfileDto)
        {
            return new UserProfile
            {
                FirstName = userProfileDto.FirstName,
                LastName = userProfileDto.LastName,
                DisplayName = userProfileDto.DisplayName,
                Bio = userProfileDto.Bio,
                ProfileUrlImageUrl = userProfileDto.ProfileImageUrl,
                DateOfBirth = userProfileDto.DateOfBirth,
                PhoneNumber = userProfileDto.PhoneNumber,
                CreatedAt = userProfileDto.CreatedAt,
                UpdatedAt = userProfileDto.UpdatedAt,
                Email = userProfileDto.Email,
            };
        }
        
        public static UpdateUserProfileDto ToUpdateDto(this UserProfile userProfile)
        {
            return new UpdateUserProfileDto
            {
                FirstName = userProfile.FirstName,
                LastName = userProfile.LastName,
                DisplayName = userProfile.DisplayName,
                Bio = userProfile.Bio,
                ProfileImageUrl = userProfile.ProfileUrlImageUrl,
                DateOfBirth = userProfile.DateOfBirth,
                PhoneNumber = userProfile.PhoneNumber,
                Email = userProfile.Email,
            };
        }
        public static UserProfile FromUpdateDto(this UpdateUserProfileDto updateDto)
        {
            return new UserProfile
            {
                FirstName = updateDto.FirstName,
                LastName = updateDto.LastName,
                DisplayName = updateDto.DisplayName,
                Bio = updateDto.Bio,
                ProfileUrlImageUrl = updateDto.ProfileImageUrl,
                DateOfBirth = updateDto.DateOfBirth,
                PhoneNumber = updateDto.PhoneNumber,
                Email = updateDto.Email,
            };
        }

        // reflection-based method to update UserProfile from UpdateUserProfileDto
        // TODO: consider rewriting this method to be apply to all updatable DTOs
        public static void updateUserProfile(this UpdateUserProfileDto updateDto, UserProfile userProfile)
        {
            var dtoProperties = updateDto.GetType().GetProperties();
            var entityProperties = userProfile.GetType().GetProperties().ToDictionary(p => p.Name, p => p);

            foreach (var dtoProperty in dtoProperties)
            {
                var dtoValue = dtoProperty.GetValue(updateDto);
                if (dtoValue != null && entityProperties.TryGetValue(
                    dtoProperty.Name, out var entityProperty))
                {
                    if (dtoValue is string strValue
                        && string.IsNullOrWhiteSpace(strValue))
                    {
                        continue; // Skip empty strings
                    }
                    if (entityProperty.CanWrite
                        && entityProperty.PropertyType == dtoProperty.PropertyType)
                    {
                        // Set the value only if the property is writable and types match
                        entityProperty.SetValue(userProfile, dtoValue);
                    }

                }
            }
        }

        public static UserProfileDto ToDto(this UpdateUserProfileDto updateDto)
        {
            return new UserProfileDto
            {
                FirstName = updateDto.FirstName,
                LastName = updateDto.LastName,
                DisplayName = updateDto.DisplayName,
                Bio = updateDto.Bio,
                ProfileImageUrl = updateDto.ProfileImageUrl,
                DateOfBirth = updateDto.DateOfBirth,
                PhoneNumber = updateDto.PhoneNumber,
                Email = updateDto.Email,
            };
        }
    }
}