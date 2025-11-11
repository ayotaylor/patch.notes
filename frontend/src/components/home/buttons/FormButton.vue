<template>
  <ButtonComponent
    :type="type"
    :title="title"
    :variant="variant"
    :size="size"
    :disabled="disabled"
    :loading="loading"
    :full-width="fullWidth"
    :icon-size="iconSize"
    @click="handleClick"
  >
    <template #icon-left v-if="$slots['icon-left']">
      <slot name="icon-left"></slot>
    </template>
    <template #icon-right v-if="$slots['icon-right']">
      <slot name="icon-right"></slot>
    </template>
  </ButtonComponent>
</template>

<script setup>
import ButtonComponent from './ButtonComponent.vue'

defineProps({
  // Button text
  title: {
    type: String,
    required: true
  },
  // Button type: 'button', 'submit', or 'reset'
  type: {
    type: String,
    default: 'submit',
    validator: (value) => ['button', 'submit', 'reset'].includes(value)
  },
  // Variant: 'primary' or 'secondary'
  variant: {
    type: String,
    default: 'primary',
    validator: (value) => ['primary', 'secondary'].includes(value)
  },
  // Size: 'sm', 'md', 'lg'
  size: {
    type: String,
    default: 'lg',
    validator: (value) => ['sm', 'md', 'lg'].includes(value)
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
  // Full width button
  fullWidth: {
    type: Boolean,
    default: true
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
  emit('click', event)
}
</script>
