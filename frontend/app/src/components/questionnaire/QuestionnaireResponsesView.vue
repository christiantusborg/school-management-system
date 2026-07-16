<template>
  <v-card class="responses-view">
    <v-card-title class="d-flex align-center justify-space-between">
      <div class="d-flex align-center">
        <v-icon icon="mdi-file-chart" class="mr-2" />
        Responses
      </div>
      <div class="d-flex gap-2">
        <v-btn
          icon="mdi-download"
          size="small"
          variant="tonal"
          @click="exportResponses"
        />
        <v-btn
          icon="mdi-refresh"
          size="small"
          variant="tonal"
          @click="refreshResponses"
        />
      </div>
    </v-card-title>

    <v-card-text>
      <!-- Filters -->
      <div class="d-flex gap-4 mb-4">
        <v-text-field
          v-model="searchQuery"
          label="Search responses"
          prepend-inner-icon="mdi-magnify"
          variant="outlined"
          density="compact"
          clearable
        />
        <v-select
          v-model="dateFilter"
          :items="dateFilterOptions"
          label="Date Range"
          variant="outlined"
          density="compact"
        />
      </div>

      <!-- Summary Stats -->
      <v-row class="mb-4">
        <v-col cols="12" md="3">
          <v-card variant="tonal" color="primary">
            <v-card-text class="text-center">
              <div class="text-h4">{{ totalResponses }}</div>
              <div class="text-caption">Total Responses</div>
            </v-card-text>
          </v-card>
        </v-col>
        <v-col cols="12" md="3">
          <v-card variant="tonal" color="success">
            <v-card-text class="text-center">
              <div class="text-h4">{{ completedResponses }}</div>
              <div class="text-caption">Completed</div>
            </v-card-text>
          </v-card>
        </v-col>
        <v-col cols="12" md="3">
          <v-card variant="tonal" color="warning">
            <v-card-text class="text-center">
              <div class="text-h4">{{ partialResponses }}</div>
              <div class="text-caption">Partial</div>
            </v-card-text>
          </v-card>
        </v-col>
        <v-col cols="12" md="3">
          <v-card variant="tonal" color="info">
            <v-card-text class="text-center">
              <div class="text-h4">{{ averageTime }}</div>
              <div class="text-caption">Avg. Time (min)</div>
            </v-card-text>
          </v-card>
        </v-col>
      </v-row>

      <!-- Responses Table -->
      <v-data-table
        :headers="headers"
        :items="filteredResponses"
        :loading="isLoading"
        class="elevation-1"
      >
        <template #item.submittedAt="{ item }">
          {{ formatDate(item.submittedAt) }}
        </template>

        <template #item.status="{ item }">
          <v-chip
            :color="getStatusColor(item.status)"
            size="small"
          >
            {{ item.status }}
          </v-chip>
        </template>

        <template #item.actions="{ item }">
          <v-btn
            icon="mdi-eye"
            size="small"
            variant="text"
            @click="viewResponse(item)"
          />
          <v-btn
            icon="mdi-download"
            size="small"
            variant="text"
            @click="downloadResponse(item)"
          />
        </template>

        <template #no-data>
          <div class="text-center py-8">
            <v-icon size="64" color="grey-lighten-1" class="mb-4">
              mdi-file-chart-outline
            </v-icon>
            <div class="text-h6 mb-2">No responses yet</div>
            <div class="text-body-2 text-medium-emphasis">
              Responses will appear here once people start filling out your questionnaire
            </div>
          </div>
        </template>
      </v-data-table>
    </v-card-text>

    <!-- Response Detail Dialog -->
    <v-dialog v-model="showDetailDialog" max-width="800">
      <v-card v-if="selectedResponse">
        <v-card-title>Response Details</v-card-title>
        <v-card-text>
          <div class="mb-4">
            <strong>Submitted:</strong> {{ formatDate(selectedResponse.submittedAt) }}
          </div>
          <div class="mb-4">
            <strong>Status:</strong>
            <v-chip :color="getStatusColor(selectedResponse.status)" size="small" class="ml-2">
              {{ selectedResponse.status }}
            </v-chip>
          </div>
          <v-divider class="mb-4" />
          <div class="response-data">
            <pre>{{ JSON.stringify(selectedResponse.answers, null, 2) }}</pre>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="showDetailDialog = false">
            Close
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-card>
</template>

<script setup lang="ts">
// -nocheck — ported SysCase file; TS strict cleanup is a follow-up
import { ref, computed, onMounted } from 'vue'

// Props
const props = defineProps<{
  questionnaireId?: string
}>()

// Reactive state
const searchQuery = ref('')
const dateFilter = ref('all')
const isLoading = ref(false)
const showDetailDialog = ref(false)
const selectedResponse = ref<any>(null)

// Mock data
const responses = ref([
  {
    id: '1',
    submittedAt: '2024-01-15T10:30:00Z',
    status: 'completed',
    answers: {
      'field1': 'John Doe',
      'field2': 'john@example.com',
      'field3': 'I am interested in your services'
    }
  },
  {
    id: '2',
    submittedAt: '2024-01-14T15:20:00Z',
    status: 'partial',
    answers: {
      'field1': 'Jane Smith',
      'field2': 'jane@example.com'
    }
  }
])

// Computed
const dateFilterOptions = [
  { title: 'All Time', value: 'all' },
  { title: 'Last 7 days', value: '7d' },
  { title: 'Last 30 days', value: '30d' },
  { title: 'Last 90 days', value: '90d' }
]

const headers = [
  { title: 'ID', value: 'id', width: '100px' },
  { title: 'Submitted', value: 'submittedAt', width: '150px' },
  { title: 'Status', value: 'status', width: '120px' },
  { title: 'Actions', value: 'actions', width: '120px', sortable: false }
]

const filteredResponses = computed(() => {
  let filtered = responses.value

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    filtered = filtered.filter(response =>
      response.id.toLowerCase().includes(query) ||
      JSON.stringify(response.answers).toLowerCase().includes(query)
    )
  }

  return filtered
})

const totalResponses = computed(() => responses.value.length)
const completedResponses = computed(() => responses.value.filter(r => r.status === 'completed').length)
const partialResponses = computed(() => responses.value.filter(r => r.status === 'partial').length)
const averageTime = computed(() => '5.2') // Mock data

// Methods
const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const getStatusColor = (status: string) => {
  switch (status) {
    case 'completed': return 'success'
    case 'partial': return 'warning'
    case 'abandoned': return 'error'
    default: return 'grey'
  }
}

const viewResponse = (response: any) => {
  selectedResponse.value = response
  showDetailDialog.value = true
}

const downloadResponse = (response: any) => {
  const dataStr = JSON.stringify(response, null, 2)
  const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr)

  const exportFileDefaultName = `response-${response.id}.json`

  const linkElement = document.createElement('a')
  linkElement.setAttribute('href', dataUri)
  linkElement.setAttribute('download', exportFileDefaultName)
  linkElement.click()
}

const exportResponses = () => {
  const dataStr = JSON.stringify(filteredResponses.value, null, 2)
  const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr)

  const exportFileDefaultName = `responses-${props.questionnaireId || 'export'}.json`

  const linkElement = document.createElement('a')
  linkElement.setAttribute('href', dataUri)
  linkElement.setAttribute('download', exportFileDefaultName)
  linkElement.click()
}

const refreshResponses = () => {
  // In a real app, this would fetch fresh data
  console.log('Refreshing responses...')
}

// Lifecycle
onMounted(() => {
  // Load responses for the questionnaire
  if (props.questionnaireId) {
    // fetchResponses(props.questionnaireId)
  }
})
</script>

<style scoped>
.responses-view {
  height: 80vh;
  overflow-y: auto;
}

.response-data {
  background-color: rgba(var(--v-theme-surface-variant), 0.3);
  border-radius: 4px;
  padding: 12px;
  max-height: 300px;
  overflow-y: auto;
}

.response-data pre {
  margin: 0;
  font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
  font-size: 12px;
  line-height: 1.4;
}
</style>