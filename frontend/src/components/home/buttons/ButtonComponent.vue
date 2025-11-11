<template>
  <component
    :is="componentType"
    :to="to"
    :href="href"
    :type="buttonType"
    :disabled="disabled"
    :class="buttonClasses"
    @click="handleClick"
  >
    <span v-if="loading" class="inline-block border-2 border-current border-t-transparent rounded-full animate-spin" :class="[loadingIconSizeClasses, iconSpacing]"></span>
    <span v-if="!loading && $slots['icon-left']" :class="[iconSizeClasses, iconSpacing]">
      <slot name="icon-left"></slot>
    </span>
    <span v-if="title">{{ title }}</span>
    <span v-if="!loading && $slots['icon-right']" :class="[iconSizeClasses, title ? 'ml-2' : '']">
      <slot name="icon-right"></slot>
    </span>
  </component>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  // Button text
  title: {
    type: String,
    required: true
  },
  // Variant: 'primary' (dark bg, white text) or 'secondary' (light bg, border)
  variant: {
    type: String,
    default: 'primary',
    validator: (value) => ['primary', 'secondary'].includes(value)
  },
  // Size: 'sm', 'md', 'lg'
  size: {
    type: String,
    default: 'md',
    validator: (value) => ['sm', 'md', 'lg'].includes(value)
  },
  // Button type (for form buttons) - 'button', 'submit', or 'reset'
  type: {
    type: String,
    default: 'button',
    validator: (value) => ['button', 'submit', 'reset'].includes(value)
  },
  // Disabled state
  disabled: {
    type: Boolean,
    default: false
  },
  // Loading state (shows spinner)
  loading: {
    type: Boolean,
    default: false
  },
  // Router link 'to' prop
  to: {
    type: [String, Object],
    default: null
  },
  // External link href
  href: {
    type: String,
    default: null
  },
  // Full width button
  fullWidth: {
    type: Boolean,
    default: false
  },
  // Icon size: 'sm', 'md', 'lg'
  iconSize: {
    type: String,
    default: 'md',
    validator: (value) => ['sm', 'md', 'lg'].includes(value)
  }
})

const emit = defineEmits(['click'])

const handleClick = (event) => {
  if (!props.disabled && !props.loading) {
    emit('click', event)
  }
}

// Determine component type
const componentType = computed(() => {
  if (props.to) return 'router-link'
  if (props.href) return 'a'
  return 'button'
})

// Determine button type attribute (only for actual buttons)
const buttonType = computed(() => {
  if (props.to || props.href) return undefined
  return props.type
})

const buttonClasses = computed(() => {
  const classes = [
    // Base styles
    'inline-flex items-center justify-center',
    'font-roboto font-bold tracking-wide rounded',
    'transition-all duration-200',
    'focus:outline-none focus:ring-2 focus:ring-offset-2',
    'disabled:opacity-50 disabled:cursor-not-allowed'
  ]

  // Width
  if (props.fullWidth) {
    classes.push('w-full')
  }

  // Size variants with responsive widths
  if (props.size === 'sm') {
    classes.push('h-8 min-w-20 sm:min-w-24 px-3 text-xs whitespace-nowrap')
  } else if (props.size === 'md') {
    classes.push('h-9 min-w-28 sm:min-w-32 px-4 text-sm whitespace-nowrap')
  } else if (props.size === 'lg') {
    classes.push('h-12 min-w-32 sm:min-w-40 px-6 text-base whitespace-nowrap')
  }

  // Variant styles
  if (props.variant === 'primary') {
    classes.push(
      'bg-theme-btn-primary dark:bg-theme-btn-primary-dark',
      'text-white',
      'hover:opacity-90',
      'focus:ring-theme-btn-primary dark:focus:ring-theme-btn-primary-dark'
    )
  } else if (props.variant === 'secondary') {
    classes.push(
      'bg-theme-btn-secondary dark:bg-theme-btn-secondary-dark',
      'text-theme-text-primary dark:text-theme-text-primary-dark',
      'border border-theme-border dark:border-theme-border-dark',
      'hover:bg-gray-50 dark:hover:bg-gray-700',
      'focus:ring-theme-border dark:focus:ring-theme-border-dark'
    )
  }

  return classes.join(' ')
})

const iconSpacing = computed(() => {
  return props.title ? 'mr-2' : ''
})

// Icon size classes
const iconSizeClasses = computed(() => {
  const sizeMap = {
    sm: 'w-4 h-4',
    md: 'w-5 h-5',
    lg: 'w-6 h-6'
  }
  return sizeMap[props.iconSize]
})

// Loading icon size should match the icon size
const loadingIconSizeClasses = computed(() => {
  return iconSizeClasses.value
})
</script>
