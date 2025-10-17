<script setup>
import HomeHeader from './HeaderBar.vue'
import HomeNavigation from './NavigationBar.vue'

const recentGames = [
  {
    title: 'Stellar Blade',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/5b352c98b2010576119cea8b881d4e255e61691f?width=389',
  },
  {
    title: 'Final Fantasy VII Rebirth',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/2403265359b7360f8fa23191410ef2a8d6220254?width=389',
  },
  {
    title: 'Elden Ring',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/b57eff94a771a7bbd7a49146351932d115548b1c?width=389',
  },
  {
    title: 'Helldivers 2',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/3a2cb97c37806475d077d3802823f685321d0a0b?width=389',
  },
  {
    title: "Dragon's Dogma 2",
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/4898fc79de31209bd2ee4b907bd4bc308e7c8b56?width=389',
  },
  {
    title: "Baldur's Gate 3",
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/50d858b3ca9f3fb73a01db6ee80a38dc78743753?width=389',
  },
]

const popularReviews = [
  {
    title: 'A Masterpiece of Open-World\nStorytelling',
    author: 'Jane Doe',
    game: 'Elden Ring',
    excerpt:
      'FromSoftware has once again redefined the genre, delivering an unforgettable experience that will be talked about for years to come...',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/0bd39325f25778f231b679c1a37e88c27ae79953?width=486',
  },
  {
    title: 'More Than a Remake, A Reimagining',
    author: 'John Smith',
    game: 'Final Fantasy VII Rebirth',
    excerpt:
      'Rebirth takes the beloved classic and expands upon it in every way imaginable, creating a new yet familiar journey...',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/6fd4291a4679906e1e6abf2bceabdd785ba1260e?width=486',
  },
  {
    title: 'A Stylish and Thrilling Action RPG',
    author: 'Alex Ray',
    game: 'Stellar Blade',
    excerpt:
      'Stellar Blade delivers fast-paced combat and a visually stunning world, making it a must-play for fans of the genre...',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/7c6219d978919ed031addaf3a55ff9879859eb41?width=486',
  },
  {
    title: 'The Pinnacle of Co-op Shooters',
    author: 'Sarah Chen',
    game: 'Helldivers 2',
    excerpt:
      'Helldivers 2 offers chaotic, hilarious, and deeply satisfying cooperative gameplay that keeps you coming back for more...',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/5d5d4b1d2ba265fa799a8e609a42c8e4dca4104c?width=486',
  },
]

const popularLists = [
  {
    title: 'The Greatest RPGs of All Time',
    author: 'GamerGirl92',
    likes: '1.2k',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/543add445417e596a718defbf54e4aa007a27560?width=800',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/0d9d0255c0c70c57840eac6244ec7935cc307d23?width=48',
  },
  {
    title: 'Indie Gems You Might Have Missed',
    author: 'RetroKing',
    likes: '876',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/3bbca8c9e6b6c5ae4b954e5e47ffd98702adce35?width=800',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/c42469ab39146cb23888fcc95433e76c06a8ec78?width=48',
  },
  {
    title: 'Hardest Games Ever Made',
    author: 'SoulslikeFan',
    likes: '2.5k',
    image: 'https://api.builder.io/api/v1/image/assets/TEMP/9d74285b6f34ccd2fdc9f81402f5ded8cc026dc5?width=800',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/001a48cccf735939c0b4babf3fb27bfcbd4f319d?width=48',
  },
]

const popularMembers = [
  {
    name: 'Jane Doe',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/55268c3c8278136914ad3fc2c8674959eedf6bde?width=192',
  },
  {
    name: 'John Smith',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/e91e04677d2fb3dd66f0821d210c18bb5acf9dfc?width=192',
  },
  {
    name: 'Alex Ray',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/36f7fe8b6319dee36537570df19423e5a9839fc0?width=192',
  },
  {
    name: 'Sarah Chen',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/4168d38c2da8f88eb2ceeb275fc9b4fe28bd94df?width=192',
  },
  {
    name: 'Emily Carter',
    avatar: 'https://api.builder.io/api/v1/image/assets/TEMP/22a1fed35e0669138b94ae7aaf974e82e9c56bcc?width=192',
  },
]
</script>

<template>
  <div class="min-h-screen bg-[#F6F7F7]">
    <!-- Header Component -->
    <HomeHeader />

    <!-- Navigation Component -->
    <HomeNavigation />

    <!-- Hero Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280">
        <div class="relative h-[520px] rounded-lg flex items-end justify-center bg-cover bg-center" style="
            background-image: linear-gradient(180deg, rgba(0, 0, 0, 0.5) 0%, rgba(0, 0, 0, 0.7) 100%),
              url('https://api.builder.io/api/v1/image/assets/TEMP/709559efbb027911fac46667344019496767a351?width=2560');
          ">
          <div class="text-center px-4 max-w-3xl pb-16">
            <h2 class="text-white font-newsreader text-2xl md:text-4xl font-light leading-tight tracking-tight mb-6">
              Track the games you've played.<br />
              Save the ones you want to play.<br />
              Tell your friends what's good.
            </h2>
            <button
              class="h-12 min-w-21 px-8 bg-white text-cod-gray border border-cod-gray font-roboto font-bold text-lg tracking-wide rounded">
              Sign Up
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Recently Reviewed Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280 py-8">
        <h3 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300">
          Recently Reviewed
        </h3>
        <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-6 overflow-x-auto">
          <div v-for="game in recentGames" :key="game.title" class="flex flex-col gap-3">
            <img :src="game.image" :alt="game.title" class="w-full aspect-[3/4] object-cover rounded-lg" />
            <p class="font-tinos text-base text-cod-gray">{{ game.title }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Popular Reviews Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280 py-8">
        <h3 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300">Popular Reviews</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div v-for="(review, index) in popularReviews" :key="index" class="flex flex-col md:flex-row gap-6">
            <div class="flex-1">
              <h4 class="font-newsreader text-xl font-bold text-cod-gray mb-1 whitespace-pre-line">
                {{ review.title }}
              </h4>
              <p class="font-tinos text-sm text-river-bed mb-1">
                <span class="text-river-bed">By {{ review.author }} on </span>
                <span class="text-cod-gray">{{ review.game }}</span>
              </p>
              <p class="font-tinos text-base text-ebony-clay leading-6 mt-2">{{ review.excerpt }}</p>
            </div>
            <img :src="review.image" :alt="review.game" class="w-full md:w-32 aspect-[3/4] object-cover rounded-lg" />
          </div>
        </div>
      </div>
    </div>

    <!-- Popular Lists Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8">
      <div class="w-full max-w-1280 py-8">
        <h3 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300">Popular Lists</h3>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div v-for="(list, index) in popularLists" :key="index" class="flex flex-col">
            <img :src="list.image" :alt="list.title" class="w-full aspect-video object-cover rounded-lg mb-3" />
            <h4 class="font-newsreader text-lg font-bold text-cod-gray mb-2">{{ list.title }}</h4>
            <div class="flex items-center gap-2">
              <img :src="list.avatar" :alt="list.author" class="w-6 h-6 rounded-full" />
              <span class="font-tinos text-sm text-river-bed">{{ list.author }}</span>
              <svg class="w-4 h-5" viewBox="0 0 17 21" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path
                  d="M12.6879 16.8906H5.35454V8.22396L10.0212 3.55729L10.8545 4.39062C10.9323 4.4684 10.9962 4.57396 11.0462 4.70729C11.0962 4.84062 11.1212 4.9684 11.1212 5.09062V5.32396L10.3879 8.22396H14.6879C15.0434 8.22396 15.3545 8.35729 15.6212 8.62396C15.8879 8.89062 16.0212 9.20174 16.0212 9.55729V10.8906C16.0212 10.9684 16.0101 11.0517 15.9879 11.1406C15.9657 11.2295 15.9434 11.3128 15.9212 11.3906L13.9212 16.0906C13.8212 16.3128 13.6545 16.5017 13.4212 16.6573C13.1879 16.8128 12.9434 16.8906 12.6879 16.8906ZM6.68788 15.5573H12.6879L14.6879 10.8906V9.55729H8.68788L9.58788 5.89062L6.68788 8.79062V15.5573ZM6.68788 8.79062V9.55729V10.8906V15.5573V8.79062ZM5.35454 8.22396V9.55729H3.35454V15.5573H5.35454V16.8906H2.02121V8.22396H5.35454Z"
                  fill="#4B5563" />
              </svg>
              <span class="font-tinos text-sm text-river-bed">{{ list.likes }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Popular Members Section -->
    <div class="flex justify-center px-4 md:px-8 lg:px-40 mt-8 pb-16">
      <div class="w-full max-w-1280 py-8">
        <h3 class="font-newsreader text-2xl font-bold text-cod-gray mb-4 border-b border-gray-300">Popular Members</h3>
        <div class="flex flex-wrap justify-center gap-8">
          <div v-for="(member, index) in popularMembers" :key="index" class="flex flex-col items-center gap-2">
            <img :src="member.avatar" :alt="member.name" class="w-24 h-24 rounded-full" />
            <p class="font-tinos text-base text-cod-gray text-center">{{ member.name }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
