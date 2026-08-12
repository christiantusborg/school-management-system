import { ref } from 'vue'

// Shared across every LetterButtonsRow on the page. During the enum→dynamic
// letter migration both systems exist side by side; hide the OLD built-in
// (enum) letters by default so the new config-created letters stand out, and
// let one toggle reveal/collapse the old ones everywhere at once.
export const showOldLetters = ref(false)
