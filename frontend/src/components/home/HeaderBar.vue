<template>
  <div class="flex justify-center px-4 md:px-8 lg:px-40">
    <div class="w-full max-w-1280">
      <!-- Top Bar -->
      <div class="flex items-center justify-between border-b border-gray-300 py-2">
        <!-- Search Bar -->
        <div class="flex items-stretch h-9 min-w-40 max-w-64 rounded-md overflow-hidden bg-gray-100">
          <div class="flex items-center justify-center pl-3 bg-gray-100">
            <svg class="w-6 h-7" viewBox="0 0 25 29" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M20.0868 23.5807L13.9618 17.4557C13.4757 17.8446 12.9166 18.1524 12.2847 18.3793C11.6527 18.6062 10.9803 18.7196 10.2673 18.7196C8.50112 18.7196 7.00633 18.1079 5.78295 16.8845C4.55957 15.6611 3.94788 14.1663 3.94788 12.4001C3.94788 10.6339 4.55957 9.13914 5.78295 7.91576C7.00633 6.69238 8.50112 6.08069 10.2673 6.08069C12.0335 6.08069 13.5283 6.69238 14.7517 7.91576C15.9751 9.13914 16.5868 10.6339 16.5868 12.4001C16.5868 13.1131 16.4733 13.7855 16.2465 14.4175C16.0196 15.0494 15.7118 15.6085 15.3229 16.0946L21.4479 22.2196L20.0868 23.5807ZM10.2673 16.7751C11.4826 16.7751 12.5156 16.3498 13.3663 15.4991C14.217 14.6484 14.6423 13.6154 14.6423 12.4001C14.6423 11.1849 14.217 10.1519 13.3663 9.30117C12.5156 8.45048 11.4826 8.02513 10.2673 8.02513C9.05204 8.02513 8.01906 8.45048 7.16836 9.30117C6.31767 10.1519 5.89232 11.1849 5.89232 12.4001C5.89232 13.6154 6.31767 14.6484 7.16836 15.4991C8.01906 16.3498 9.05204 16.7751 10.2673 16.7751Z"
                fill="#6B7280"
              />
            </svg>
          </div>
          <input
            type="text"
            placeholder="Search"
            class="flex-1 px-2 bg-gray-100 text-sm font-tinos text-pale-sky outline-none"
          />
        </div>

        <!-- Auth Buttons -->
        <div class="flex items-center gap-2">
          <button
            class="h-9 min-w-21 px-5 bg-cod-gray text-white font-roboto font-bold text-sm tracking-wide rounded"
          >
            Create Account
          </button>
          <button
            class="h-9 min-w-21 px-5 bg-white text-cod-gray border border-cod-gray font-roboto font-bold text-sm tracking-wide rounded"
          >
            Log In
          </button>
        </div>
      </div>

      <!-- Title Bar -->
      <div
        class="flex flex-col md:flex-row items-center justify-between border-b border-gray-300 py-8 gap-4"
      >
        <div class="text-sm font-tinos text-river-bed">Monday, May 20, 2024</div>
        <h1 class="text-5xl md:text-7xl font-newsreader font-bold text-cod-gray">Patchnotes*</h1>
        <div class="text-sm font-tinos text-river-bed">Today in video games</div>
      </div>
    </div>
  </div>
</template>

<script setup>
// No props or state needed for now
</script>
