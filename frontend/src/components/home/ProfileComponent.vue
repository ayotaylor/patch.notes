<template>
  <div class="min-h-screen bg-theme-bg-primary dark:bg-theme-bg-primary-dark transition-colors duration-200 py-8 px-4">
    <!-- Loading State -->
    <div v-if="loading" class="flex flex-col items-center justify-center py-20">
      <div class="w-12 h-12 border-4 border-theme-btn-primary dark:border-theme-btn-primary-dark border-t-transparent rounded-full animate-spin"></div>
      <p class="mt-4 font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark">Loading profile...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="max-w-2xl mx-auto">
      <div class="p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
        <div class="flex items-center">
          <svg class="w-5 h-5 text-red-600 dark:text-red-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 2L1 21h22L12 2zm0 3.83L19.53 19H4.47L12 5.83zM11 16h2v2h-2v-2zm0-6h2v4h-2v-4z"/>
          </svg>
          <span class="font-tinos text-sm text-red-800 dark:text-red-300">{{ error }}</span>
        </div>
      </div>
    </div>

    <!-- Profile Content -->
    <div v-else-if="profile" class="max-w-6xl mx-auto space-y-6">
      <!-- Profile Header -->
      <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-sm border border-theme-border dark:border-theme-border-dark transition-colors duration-200">
        <div class="p-6">
          <div class="flex flex-col md:flex-row gap-6">
            <!-- Profile Image -->
            <div class="flex-shrink-0">
              <div class="relative inline-block">
                <img
                  :src="profile.profileImageUrl || defaultAvatar"
                  :alt="`${profile.displayName || 'User'}'s profile`"
                  class="w-32 h-32 rounded-full border-4 border-theme-border dark:border-theme-border-dark shadow-lg object-cover"
                >
                <button
                  v-if="isOwnProfile && isEditing"
                  @click="triggerImageUpload"
                  class="absolute bottom-0 right-0 w-10 h-10 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded-full flex items-center justify-center hover:opacity-90 transition-opacity shadow-lg"
                  title="Change profile picture"
                >
                  <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 12.75c1.63 0 3.07.39 4.24.9 1.08.48 1.76 1.56 1.76 2.73V18H6v-1.61c0-1.18.68-2.26 1.76-2.73 1.17-.52 2.61-.91 4.24-.91zM4 13h3v-2.5H4V8l-3 4 3 4v-3zm16-3h-3v2.5h3V15l3-4-3-4v3zm-8-7c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z"/>
                  </svg>
                </button>
                <input
                  ref="imageInput"
                  type="file"
                  accept="image/*"
                  @change="handleImageUpload"
                  class="hidden"
                >
              </div>
            </div>

            <!-- Profile Info -->
            <div class="flex-grow">
              <div class="flex flex-col sm:flex-row justify-between items-start gap-4 mb-4">
                <div class="flex-grow">
                  <!-- Display Name -->
                  <h1 v-if="!isEditing" class="font-newsreader text-3xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark mb-1">
                    {{ profile.displayName || fullName || 'User' }}
                  </h1>
                  <input
                    v-else
                    v-model="editForm.displayName"
                    type="text"
                    class="w-full px-3 py-2 font-newsreader text-3xl font-bold bg-transparent text-theme-text-primary dark:text-theme-text-primary-dark border-b-2 border-theme-border dark:border-theme-border-dark focus:outline-none focus:border-theme-btn-primary dark:focus:border-theme-btn-primary-dark"
                    placeholder="Display name"
                    maxlength="100"
                  >

                  <!-- Full Name -->
                  <p v-if="!isEditing && fullName && fullName !== profile.displayName" class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">
                    {{ fullName }}
                  </p>
                  <div v-else-if="isEditing" class="grid grid-cols-2 gap-3 mb-2">
                    <input
                      v-model="editForm.firstName"
                      type="text"
                      class="px-3 py-2 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark"
                      placeholder="First name"
                      maxlength="50"
                    >
                    <input
                      v-model="editForm.lastName"
                      type="text"
                      class="px-3 py-2 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark"
                      placeholder="Last name"
                      maxlength="50"
                    >
                  </div>

                  <!-- Email -->
                  <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark flex items-center">
                    <svg class="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                      <path d="M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/>
                    </svg>
                    {{ profile.email }}
                  </p>
                </div>

                <!-- Action Buttons -->
                <div class="flex-shrink-0">
                  <!-- Follow Button for Other Users -->
                  <div v-if="!isOwnProfile && authStore.user" class="mb-2">
                    <button
                      @click="toggleFollow"
                      :disabled="followingInProgress"
                      class="px-4 py-2 rounded-lg font-tinos text-base font-medium transition-all duration-200 flex items-center"
                      :class="isFollowed
                        ? 'bg-green-600 hover:bg-green-700 text-white'
                        : 'bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white hover:opacity-90'"
                    >
                      <span v-if="followingInProgress" class="inline-block w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2"></span>
                      <svg v-else class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                        <path v-if="isFollowed" d="M9 16.2L4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2z"/>
                        <path v-else d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
                      </svg>
                      {{ isFollowed ? 'Following' : 'Follow' }}
                    </button>
                  </div>

                  <!-- Login Prompt for Non-Authenticated Users -->
                  <div v-else-if="!isOwnProfile && !authStore.user" class="mb-2">
                    <router-link
                      to="/login"
                      class="px-4 py-2 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg font-tinos text-base hover:bg-theme-border dark:hover:bg-theme-border-dark transition-all duration-200 flex items-center"
                    >
                      <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                        <path d="M10.09 15.59L11.5 17l5-5-5-5-1.41 1.41L12.67 11H3v2h9.67l-2.58 2.59zM19 3H5c-1.11 0-2 .9-2 2v4h2V5h14v14H5v-4H3v4c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2z"/>
                      </svg>
                      Login to Follow
                    </router-link>
                  </div>

                  <!-- Edit Profile Button for Own Profile -->
                  <div v-if="isOwnProfile">
                    <template v-if="!isEditing">
                      <button
                        @click="startEditing"
                        class="px-4 py-2 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg font-tinos text-base hover:bg-theme-border dark:hover:bg-theme-border-dark transition-all duration-200 flex items-center"
                      >
                        <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                          <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
                        </svg>
                        Edit Profile
                      </button>
                    </template>
                    <template v-else>
                      <div class="flex gap-2">
                        <button
                          @click="saveProfile"
                          :disabled="isSaving"
                          class="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg font-tinos text-base transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center"
                        >
                          <span v-if="isSaving" class="inline-block w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin mr-2"></span>
                          <svg v-else class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                            <path d="M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z"/>
                          </svg>
                          {{ isSaving ? 'Saving...' : 'Save' }}
                        </button>
                        <button
                          @click="cancelEditing"
                          class="px-4 py-2 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg font-tinos text-base hover:bg-theme-border dark:hover:bg-theme-border-dark transition-all duration-200 flex items-center"
                        >
                          <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                            <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
                          </svg>
                          Cancel
                        </button>
                      </div>
                    </template>
                  </div>
                </div>
              </div>

              <!-- Bio -->
              <div class="mb-4">
                <label v-if="isEditing" class="block font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark mb-1">Bio</label>
                <p v-if="!isEditing && profile.bio" class="font-tinos text-base text-theme-text-primary dark:text-theme-text-primary-dark">{{ profile.bio }}</p>
                <p v-else-if="!isEditing && !profile.bio" class="font-tinos text-base text-theme-text-secondary dark:text-theme-text-secondary-dark italic">
                  No bio available
                </p>
                <textarea
                  v-else
                  v-model="editForm.bio"
                  class="w-full px-3 py-2 font-tinos text-base bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg focus:outline-none focus:ring-2 focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark resize-none"
                  rows="3"
                  placeholder="Tell us about yourself..."
                  maxlength="500"
                ></textarea>
                <div v-if="isEditing" class="text-right">
                  <span class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">
                    {{ (editForm.bio || '').length }}/500
                  </span>
                </div>
              </div>

              <!-- Profile Stats -->
              <div class="flex gap-8">
                <div class="text-center">
                  <div class="font-newsreader text-2xl font-bold text-blue-600 dark:text-blue-400">{{ profile.gamesCount || 0 }}</div>
                  <div class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Games</div>
                </div>
                <div class="text-center">
                  <div class="font-newsreader text-2xl font-bold text-green-600 dark:text-green-400">{{ profile.achievementsCount || 0 }}</div>
                  <div class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Achievements</div>
                </div>
                <div class="text-center">
                  <div class="font-newsreader text-2xl font-bold text-yellow-600 dark:text-yellow-400">{{ profile.totalPlayTime || '0h' }}</div>
                  <div class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Play Time</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Top 5 Games Section -->
      <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-sm border border-theme-border dark:border-theme-border-dark transition-colors duration-200">
        <div class="p-6 border-b border-theme-border dark:border-theme-border-dark">
          <div class="flex justify-between items-center">
            <div>
              <h3 class="font-newsreader text-xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark flex items-center">
                <svg class="w-6 h-6 text-red-600 dark:text-red-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
                </svg>
                Top 5 Favorite Games
              </h3>
              <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark mt-1">Showcase your favorite games (1-5 games)</p>
            </div>
            <button
              v-if="isOwnProfile && !isEditing"
              @click="toggleGamesEditing"
              class="px-4 py-2 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg font-tinos text-sm hover:bg-theme-border dark:hover:bg-theme-border-dark transition-all duration-200 flex items-center"
            >
              <svg class="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z"/>
              </svg>
              Edit Favorites
            </button>
          </div>
        </div>

        <div class="p-6">
          <!-- No Games State -->
          <div v-if="!topGames || topGames.length === 0" class="text-center py-12">
            <svg class="w-16 h-16 text-theme-text-secondary dark:text-theme-text-secondary-dark mx-auto mb-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
            </svg>
            <h5 class="font-newsreader text-lg font-bold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">No favorite games yet</h5>
            <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark mb-4">
              {{ isOwnProfile ? 'Add 1-5 of your favorite games to showcase them on your profile.' : 'This user hasn\'t added any favorite games yet.' }}
            </p>
            <button
              v-if="isOwnProfile"
              @click="toggleGamesEditing"
              class="px-6 py-3 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded-lg font-tinos text-base hover:opacity-90 transition-all duration-200 inline-flex items-center"
            >
              <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
              </svg>
              Add Favorite Games
            </button>
          </div>

          <!-- Games List -->
          <div v-else-if="!isEditingGames" class="space-y-3">
            <div
              v-for="(game, index) in topGames"
              :key="game.id"
              class="flex items-center p-4 bg-theme-bg-primary dark:bg-theme-bg-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg transition-colors duration-200"
            >
              <!-- Rank Badge -->
              <div class="mr-4">
                <span class="w-8 h-8 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded-full flex items-center justify-center font-tinos text-sm font-bold">
                  {{ index + 1 }}
                </span>
              </div>

              <!-- Game Image -->
              <div class="mr-4">
                <img
                  :src="getImageUrl(game.primaryImageUrl, FALLBACK_TYPES.GAME_ICON, IMAGE_CONTEXTS.PROFILE_GAME)"
                  :alt="game.name"
                  class="w-16 h-16 rounded-lg object-cover"
                  @error="(e) => handleImageError(e, 'gameIcon')"
                >
              </div>

              <!-- Game Info -->
              <div class="flex-grow">
                <h6 class="font-newsreader text-lg font-bold text-theme-text-primary dark:text-theme-text-primary-dark">{{ game.name }}</h6>
              </div>
            </div>
          </div>

          <!-- Games Editing Mode -->
          <div v-else class="space-y-6">
            <!-- Game Search Component -->
            <div>
              <GameSearchComponent
                :show-card="false"
                :show-title="true"
                :show-results="true"
                :show-load-more="false"
                title="Search and Add Games"
                placeholder="Search for games..."
                results-title="Search Results"
                results-mode="compact"
                pagination-mode="infinite-scroll"
                max-height="250px"
                :auto-search="true"
                :debounce-ms="300"
                :is-game-disabled="(game) => isGameInTop5(game.id) || editTopGames.length >= 5"
                :is-game-selected="(game) => isGameInTop5(game.id)"
                @select-game="addGameToTop5"
              />
            </div>

            <!-- Current Top 5 (Editing) -->
            <div>
              <div class="flex justify-between items-center mb-3">
                <h6 class="font-newsreader text-lg font-bold text-theme-text-primary dark:text-theme-text-primary-dark">Your Top 5 Games ({{ editTopGames.length }}/5)</h6>
                <div class="text-right">
                  <span class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">
                    {{ editTopGames.length === 0 ? 'Add at least 1 game' :
                       editTopGames.length === 5 ? 'Maximum reached' :
                       `Add ${5 - editTopGames.length} more` }}
                  </span>
                </div>
              </div>
              <div v-if="editTopGames.length === 0" class="text-center py-8 bg-theme-bg-primary dark:bg-theme-bg-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg">
                <svg class="w-12 h-12 text-theme-text-secondary dark:text-theme-text-secondary-dark mx-auto mb-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
                </svg>
                <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">No games selected. Search and click games above to add them.</p>
                <p class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark mt-1">You must add between 1-5 games to your favorites.</p>
              </div>
              <div v-else class="space-y-2">
                <div
                  v-for="(game, index) in editTopGames"
                  :key="game.id"
                  class="flex items-center justify-between p-3 bg-theme-bg-primary dark:bg-theme-bg-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg"
                >
                  <div class="flex items-center flex-grow">
                    <span class="w-6 h-6 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded-full flex items-center justify-center font-tinos text-xs font-bold mr-3">{{ index + 1 }}</span>
                    <img
                      :src="getImageUrl(game.primaryImageUrl, 'gameIcon', IMAGE_CONTEXTS.PROFILE_GAME)"
                      :alt="game.name"
                      class="w-10 h-10 rounded-lg object-cover mr-3"
                      @error="(e) => handleImageError(e, 'gameIcon')"
                    >
                    <div>
                      <h6 class="font-tinos text-base font-medium text-theme-text-primary dark:text-theme-text-primary-dark">{{ game.name }}</h6>
                      <p class="font-tinos text-xs text-theme-text-secondary dark:text-theme-text-secondary-dark">{{ game.genre }}</p>
                    </div>
                  </div>
                  <div class="flex gap-2">
                    <button
                      v-if="index > 0"
                      @click="moveGameUp(index)"
                      class="p-2 bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark border border-theme-border dark:border-theme-border-dark text-theme-text-primary dark:text-theme-text-primary-dark rounded hover:bg-theme-border dark:hover:bg-theme-border-dark transition-colors duration-200"
                      title="Move up"
                    >
                      <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                        <path d="M7.41 15.41L12 10.83l4.59 4.58L18 14l-6-6-6 6z"/>
                      </svg>
                    </button>
                    <button
                      v-if="index < editTopGames.length - 1"
                      @click="moveGameDown(index)"
                      class="p-2 bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark border border-theme-border dark:border-theme-border-dark text-theme-text-primary dark:text-theme-text-primary-dark rounded hover:bg-theme-border dark:hover:bg-theme-border-dark transition-colors duration-200"
                      title="Move down"
                    >
                      <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                        <path d="M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z"/>
                      </svg>
                    </button>
                    <button
                      @click="removeGameFromTop5(index)"
                      class="p-2 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 rounded hover:bg-red-100 dark:hover:bg-red-900/30 transition-colors duration-200"
                      title="Remove"
                    >
                      <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                        <path d="M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z"/>
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Games Editing Actions -->
            <div class="flex gap-3">
              <button
                @click="saveTopGames"
                :disabled="isSavingGames || editTopGames.length === 0 || editTopGames.length > 5"
                class="px-6 py-3 bg-green-600 hover:bg-green-700 text-white rounded-lg font-tinos text-base transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center"
                :class="{ 'opacity-50': editTopGames.length === 0 }"
              >
                <span v-if="isSavingGames" class="inline-block w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin mr-2"></span>
                <svg v-else class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z"/>
                </svg>
                {{ isSavingGames ? 'Saving...' :
                   editTopGames.length === 0 ? 'Add Games First' :
                   'Save Games' }}
              </button>
              <button
                @click="cancelGamesEditing"
                class="px-6 py-3 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg font-tinos text-base hover:bg-theme-border dark:hover:bg-theme-border-dark transition-all duration-200 flex items-center"
              >
                <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/>
                </svg>
                Cancel
              </button>
            </div>

            <!-- Validation hint -->
            <div v-if="editTopGames.length === 0" class="mt-2">
              <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark flex items-center">
                <svg class="w-4 h-4 mr-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
                </svg>
                You must add at least 1 game before saving.
              </p>
            </div>
          </div>
        </div>
      </div>

      <!-- User Reviews Section -->
      <div class="bg-theme-bg-secondary dark:bg-theme-bg-secondary-dark rounded-xl shadow-sm border border-theme-border dark:border-theme-border-dark transition-colors duration-200">
        <div class="p-6 border-b border-theme-border dark:border-theme-border-dark">
          <div class="flex justify-between items-center">
            <h3 class="font-newsreader text-xl font-bold text-theme-text-primary dark:text-theme-text-primary-dark flex items-center">
              <svg class="w-6 h-6 text-yellow-600 dark:text-yellow-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/>
              </svg>
              {{ isOwnProfile ? 'My ' : ''}}Reviews
              <span v-if="userReviews.length > 0" class="font-tinos text-base font-normal text-theme-text-secondary dark:text-theme-text-secondary-dark ml-2">({{ totalReviews }})</span>
            </h3>
            <router-link
              v-if="userReviews.length > displayedReviewsLimit"
              :to="`/profile/${profile.id}/reviews`"
              class="px-4 py-2 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg font-tinos text-sm hover:bg-theme-border dark:hover:bg-theme-border-dark transition-all duration-200 flex items-center"
            >
              View All Reviews
              <svg class="w-4 h-4 ml-1" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z"/>
              </svg>
            </router-link>
          </div>
        </div>
        <div class="p-6">
          <!-- Loading State -->
          <div v-if="loadingReviews" class="flex flex-col items-center justify-center py-16">
            <div class="w-10 h-10 border-4 border-theme-btn-primary dark:border-theme-btn-primary-dark border-t-transparent rounded-full animate-spin mb-3"></div>
            <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark">Loading reviews...</p>
          </div>

          <!-- Reviews List -->
          <div v-else-if="userReviews.length > 0">
            <div class="space-y-4">
              <div
                v-for="review in displayedReviews"
                :key="review.id"
              >
                <ReviewCard
                  :review="review"
                  :show-game="true"
                  :show-date="true"
                  :truncated="true"
                  :max-length="150"
                  :is-liked="likedReviews.has(review.id)"
                  :is-processing-like="processingLikeReviews.has(review.id)"
                  @toggleLike="handleToggleLike"
                  @showComments="handleShowComments"
                />
              </div>
            </div>

            <!-- Show More Button -->
            <div v-if="userReviews.length > displayedReviewsLimit" class="text-center mt-6">
              <router-link
                :to="`/profile/${profile.id}/reviews`"
                class="px-6 py-3 bg-theme-bg-primary dark:bg-theme-bg-primary-dark text-theme-text-primary dark:text-theme-text-primary-dark border border-theme-border dark:border-theme-border-dark rounded-lg font-tinos text-base hover:bg-theme-border dark:hover:bg-theme-border-dark transition-all duration-200 inline-flex items-center"
              >
                <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                  <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z"/>
                </svg>
                View All {{ totalReviews }} Reviews
              </router-link>
            </div>
          </div>

          <!-- Empty State -->
          <div v-else class="text-center py-16">
            <svg class="w-16 h-16 text-theme-text-secondary dark:text-theme-text-secondary-dark mx-auto mb-4" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/>
            </svg>
            <h6 class="font-newsreader text-lg font-bold text-theme-text-secondary dark:text-theme-text-secondary-dark mb-2">No Reviews Yet</h6>
            <p class="font-tinos text-sm text-theme-text-secondary dark:text-theme-text-secondary-dark mb-4">
              {{ isOwnProfile ? "You haven't written any reviews yet." : `${profile.displayName || 'This user'} hasn't written any reviews yet.` }}
            </p>
            <router-link
              v-if="isOwnProfile"
              to="/dashboard"
              class="px-6 py-3 bg-theme-btn-primary dark:bg-theme-btn-primary-dark text-white rounded-lg font-tinos text-base hover:opacity-90 transition-all duration-200 inline-flex items-center"
            >
              <svg class="w-5 h-5 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                <path d="M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z"/>
              </svg>
              Find Games to Review
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- Error Alert -->
    <div v-if="saveError" class="max-w-2xl mx-auto mt-4">
      <div class="p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
        <div class="flex items-center">
          <svg class="w-5 h-5 text-red-600 dark:text-red-400 mr-2" fill="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 2L1 21h22L12 2zm0 3.83L19.53 19H4.47L12 5.83zM11 16h2v2h-2v-2zm0-6h2v4h-2v-4z"/>
          </svg>
          <span class="font-tinos text-sm text-red-800 dark:text-red-300">{{ saveError }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { defineProps } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useProfileStore } from '@/stores/profileStore'
import { useGamesStore } from '@/stores/gamesStore'
import { useToast } from 'vue-toastification'
import { useImageFallback, FALLBACK_TYPES } from '@/composables/useImageFallback'
import { useAuthRedirect } from '@/utils/authRedirect'
import GameSearchComponent from '@/components/GameSearchComponent.vue'
import ReviewCard from '@/components/ReviewCard.vue'
import { reviewsService } from '@/services/reviewsService'
import { socialService } from '@/services/socialService'
import { commentsService } from '@/services/commentsService'

// Props for viewing other users' profiles
const props = defineProps({
  userId: {
    type: String,
    default: null
  }
})

// Composables
const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const profileStore = useProfileStore()
const gamesStore = useGamesStore()
const toast = useToast()
const { handleImageError, getImageUrl, IMAGE_CONTEXTS } = useImageFallback()
const { redirectToLoginWithReturn } = useAuthRedirect()

// State
const profile = ref(null)
const loading = ref(true)
const error = ref('')
const saveError = ref('')

// Reviews state
const userReviews = ref([])
const loadingReviews = ref(false)
const totalReviews = ref(0)
const displayedReviewsLimit = 3
const likedReviews = ref(new Set())
const processingLikeReviews = ref(new Set())

// Editing state
const isEditing = ref(false)
const isSaving = ref(false)
const editForm = reactive({
  firstName: '',
  lastName: '',
  displayName: '',
  bio: '',
  profileImageUrl: ''
})

// Games state
const topGames = ref([])
const userFavorites = ref([])
const isEditingGames = ref(false)
const editTopGames = ref([])
const originalTopGames = ref([]) // Track original games for comparison
const isSavingGames = ref(false)
const imageInput = ref(null)

// Follow state
const isFollowed = ref(false)
const followingInProgress = ref(false)

// Computed properties
const profileUserId = computed(() => props.userId || route.params.userId || authStore.user?.id)
const isOwnProfile = computed(() => profileUserId.value === authStore.user?.id)

const fullName = computed(() => {
  if (!profile.value) return ''
  const { firstName, lastName } = profile.value
  return `${firstName || ''} ${lastName || ''}`.trim()
})

const defaultAvatar = computed(() => {
  const name = profile.value?.displayName || fullName.value || 'User'
  return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&size=120&background=6c757d&color=ffffff`
})

// Reviews computed properties
const displayedReviews = computed(() => {
  return userReviews.value.slice(0, displayedReviewsLimit)
})

// Methods
const fetchProfile = async () => {
  try {
    loading.value = true
    error.value = ''

    const response = await profileStore.fetchProfile(
      isOwnProfile.value ? null : profileUserId.value
    )

    profile.value = response

    // Load user's favorites to populate top games
    if (profileUserId.value) {
      await loadUserFavorites()
    }

    // Load user's reviews
    await loadUserReviews()

    // Check follow status if viewing another user's profile
    if (!isOwnProfile.value && authStore.user) {
      await checkFollowStatus()
    }
  } catch (err) {
    error.value = err.message || 'Failed to load profile'
    console.error('Error fetching profile:', err)
  } finally {
    loading.value = false
  }
}

const loadUserFavorites = async () => {
  try {
    const favorites = await gamesStore.getUserFavorites(profileUserId.value)
    userFavorites.value = favorites || []

    // Set top games from first 5 favorites
    topGames.value = userFavorites.value.slice(0, 5)
  } catch (err) {
    console.error('Error loading user favorites:', err)
    userFavorites.value = []
    topGames.value = []
  }
}

const loadUserReviews = async () => {
  if (!profileUserId.value) return

  try {
    loadingReviews.value = true
    const response = await reviewsService.getUserReviews(profileUserId.value, 1, displayedReviewsLimit + 5)
    const reviewsWithCommentCounts = await commentsService.loadCommentCountsForReviews(response.data || [])
    userReviews.value = reviewsWithCommentCounts
    totalReviews.value = response.totalCount || 0

    // Load like status for each review if current user is authenticated
    if (authStore.user) {
      likedReviews.value.clear()
      const likeStatusPromises = userReviews.value.map(async (review) => {
        try {
          const isLiked = await socialService.isReviewLiked(review.id)
          if (isLiked) {
            likedReviews.value.add(review.id)
          }
        } catch (error) {
          console.warn(`Failed to check like status for review ${review.id}:`, error)
        }
      })
      await Promise.all(likeStatusPromises)
    }
  } catch (error) {
    console.error('Error loading user reviews:', error)
    userReviews.value = []
    totalReviews.value = 0
  } finally {
    loadingReviews.value = false
  }
}

const startEditing = () => {
  isEditing.value = true
  Object.assign(editForm, {
    firstName: profile.value.firstName || '',
    lastName: profile.value.lastName || '',
    displayName: profile.value.displayName || '',
    bio: profile.value.bio || '',
    profileImageUrl: profile.value.profileImageUrl || ''
  })
}

const cancelEditing = () => {
  isEditing.value = false
  saveError.value = ''
}

const saveProfile = async () => {
  try {
    isSaving.value = true
    saveError.value = ''

    const response = await profileStore.updateProfile(editForm)

    // Update profile data
    Object.assign(profile.value, response)

    // Update auth store if it's own profile
    if (isOwnProfile.value) {
      authStore.updateUserProfileState(response)
    }

    isEditing.value = false
    toast.success('Profile updated successfully!')
  } catch (err) {
    saveError.value = err.message || 'Failed to save profile'
    console.error('Error saving profile:', err)
  } finally {
    isSaving.value = false
  }
}

const handleImageUpload = async (event) => {
  const file = event.target.files[0]
  if (!file) return

  // Validate file
  if (!file.type.startsWith('image/')) {
    toast.error('Please select a valid image file')
    return
  }

  if (file.size > 5 * 1024 * 1024) {
    toast.error('Image size must be less than 5MB')
    return
  }

  try {
    // Create preview
    const reader = new FileReader()
    reader.onload = (e) => {
      editForm.profileImageUrl = e.target.result
    }
    reader.readAsDataURL(file)

    // In a real app, upload to your server here
    // const uploadedUrl = await uploadImage(file)
    // editForm.profileImageUrl = uploadedUrl
  } catch (err) {
    toast.error('Failed to upload image')
    console.error('Error uploading image:', err)
  }
}

const triggerImageUpload = () => {
  imageInput.value?.click()
}

// Games methods
const toggleGamesEditing = () => {
  isEditingGames.value = !isEditingGames.value
  if (isEditingGames.value) {
    editTopGames.value = [...topGames.value]
    originalTopGames.value = [...topGames.value] // Store original for comparison
    // Load current favorites to show what's available
    loadUserFavorites()
  }
}

const cancelGamesEditing = () => {
  isEditingGames.value = false
  editTopGames.value = []
  originalTopGames.value = []
}

const saveTopGames = async () => {
  try {
    // Validate the games count (1-5 games required)
    if (editTopGames.value.length === 0) {
      toast.error('Please add at least 1 game to your top 5')
      return
    }

    if (editTopGames.value.length > 5) {
      toast.error('You can only have a maximum of 5 games in your top 5')
      return
    }

    isSavingGames.value = true

    // First, we need to ensure all selected games are in user's favorites
    const userId = authStore.user?.id
    if (!userId) {
      throw new Error('User not authenticated')
    }

    // For each game in editTopGames, ensure it's in favorites
    for (const game of editTopGames.value) {
      const isInFavorites = userFavorites.value.some(fav => fav.id === game.id)
      if (!isInFavorites) {
        // Add to favorites first
        await gamesStore.addToFavorites(game.id)
      }
    }

    // Remove games that were in the original top 5 but not in the new top 5
    const newTopGameIds = editTopGames.value.map(game => game.igdbId)
    const originalTopGameIds = originalTopGames.value.map(game => game.igdbId)

    for (const originalGameId of originalTopGameIds) {
      if (!newTopGameIds.includes(originalGameId)) {
        console.log('Removing game from favorites:', originalGameId)
        await gamesStore.removeFromFavorites(originalGameId)
      }
    }

    // Update the local state
    const savedGamesCount = editTopGames.value.length
    topGames.value = [...editTopGames.value]
    userFavorites.value = [...editTopGames.value]
    isEditingGames.value = false
    editTopGames.value = []
    originalTopGames.value = []

    toast.success(`Top ${savedGamesCount} games updated successfully!`)
  } catch (err) {
    toast.error('Failed to save games')
    console.error('Error saving games:', err)
  } finally {
    isSavingGames.value = false
  }
}


const addGameToTop5 = (game) => {
  if (editTopGames.value.length < 5 && !isGameInTop5(game.id)) {
    editTopGames.value.push(game)
  }
}

const removeGameFromTop5 = (index) => {
  if (index >= 0 && index < editTopGames.value.length) {
    const removedGame = editTopGames.value[index]
    editTopGames.value.splice(index, 1)
    // Optional: Show feedback to user
    console.log(`Removed "${removedGame.name}" from top games list`)
  }
}

const moveGameUp = (index) => {
  if (index > 0) {
    const game = editTopGames.value.splice(index, 1)[0]
    editTopGames.value.splice(index - 1, 0, game)
  }
}

const moveGameDown = (index) => {
  if (index < editTopGames.value.length - 1) {
    const game = editTopGames.value.splice(index, 1)[0]
    editTopGames.value.splice(index + 1, 0, game)
  }
}

const isGameInTop5 = (gameId) => {
  return editTopGames.value.some(game => game.id === gameId)
}

const handleToggleLike = async (review) => {
  if (!authStore.user) {
    toast.info('Please sign in to like reviews')
    return
  }

  const reviewId = review.id
  const wasLiked = likedReviews.value.has(reviewId)

  if (processingLikeReviews.value.has(reviewId)) return

  try {
    processingLikeReviews.value.add(reviewId)

    if (wasLiked) {
      await socialService.unlikeReview(reviewId)
      likedReviews.value.delete(reviewId)
    } else {
      await socialService.likeReview(reviewId)
      likedReviews.value.add(reviewId)
    }

    // Update like count in review
    const targetReview = userReviews.value.find(r => r.id === reviewId)
    if (targetReview) {
      targetReview.likeCount = (targetReview.likeCount || 0) + (wasLiked ? -1 : 1)
    }

  } catch (err) {
    console.error('Error toggling review like:', err)
    toast.error('Failed to update like')
  } finally {
    processingLikeReviews.value.delete(reviewId)
  }
}

const handleShowComments = (review) => {
  // Navigate to dedicated review details page
  router.push(`/reviews/${review.id}`)
}

// Follow methods
const checkFollowStatus = async () => {
  if (!profileUserId.value || isOwnProfile.value || !authStore.user) return

  try {
    const response = await socialService.isUserFollowed(profileUserId.value)
    isFollowed.value = response
  } catch (error) {
    console.error('Error checking follow status:', error)
    isFollowed.value = false
  }
}

const toggleFollow = async () => {
  if (!authStore.user) {
    redirectToLoginWithReturn('Please login to follow users')
    return
  }

  followingInProgress.value = true

  try {
    if (isFollowed.value) {
      await socialService.unfollowUser(profileUserId.value)
      isFollowed.value = false
      toast.success(`Unfollowed ${profile.value.displayName}`)
    } else {
      await socialService.followUser(profileUserId.value)
      isFollowed.value = true
      toast.success(`Now following ${profile.value.displayName}`)
    }
  } catch (error) {
    console.error('Error toggling follow:', error)
    toast.error(error.message || 'Failed to update follow status')
  } finally {
    followingInProgress.value = false
  }
}

// Watchers
watch(() => profileUserId.value, () => {
  if (profileUserId.value) {
    fetchProfile()
  }
}, { immediate: true })

// Lifecycle
onMounted(() => {
  if (profileUserId.value) {
    fetchProfile()
  }
})

// Clear search results when leaving the profile page
onBeforeUnmount(() => {
  gamesStore.clearSearchResults()
})
</script>

<style scoped>
/* Additional animations can be added here if needed */
</style>
