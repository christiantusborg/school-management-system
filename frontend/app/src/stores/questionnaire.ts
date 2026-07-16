// @ts-nocheck — ported SysCase file; TS strict cleanup is a follow-up.
//
// Phase 15e: questionnaire persistence is the QuVian encrypted IntakeApi
// (per-tenant QuestionnaireTemplate table). The full SysCase Questionnaire
// JSON is stored verbatim in the server's definitionJson column; the
// server's name + version columns are derived from the Questionnaire's
// `name.fallback` and `version` fields.
//
// On fetch, every server row is reconstituted as a SysCase Questionnaire
// with its server Guid as the client `id`. New questionnaires created via
// createNew() start with a local `questionnaire_<ts>` id and acquire a
// server Guid on the first saveCurrentQuestionnaire() call.
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type {
  Questionnaire,
  QuestionnaireItem,
  QuestionnairePage,
  QuestionnaireSection,
  QuestionnaireGroup,
  ComponentType,
  ComponentRegistry,
  LocalizableText
} from '@quvian/shared/types/questionnaire'
import { componentRegistry, groupTemplates as defaultGroupTemplates } from '@/utils/questionnaire/componentRegistry'
import { useCustomComponentsStore } from '@/stores/customComponents'
import { intakeApi } from '@quvian/shared/api/intakeApi'
import type { QuestionnaireTemplateGetResponse } from '@quvian/shared/api/intakeApi'

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
const isServerId = (id: string): boolean => GUID_RE.test(id)

const toQuestionnaire = (server: QuestionnaireTemplateGetResponse): Questionnaire => {
  let parsed: Questionnaire
  try {
    parsed = JSON.parse(server.definitionJson) as Questionnaire
  } catch (e) {
    console.warn('Failed to parse QuestionnaireTemplate.definitionJson:', e)
    parsed = {
      version: server.version || '1.0.0',
      id: server.questionnaireTemplateId,
      name: { fallback: server.name },
      pages: []
    }
  }
  return {
    ...parsed,
    id: server.questionnaireTemplateId,
    version: server.version || parsed.version || '1.0.0',
    createdAt: server.createdAt,
    updatedAt: server.modifiedAt
  }
}

// Generate unique IDs
const generateId = (prefix: string = 'item') => {
  return `${prefix}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`
}

// Helper to create localized text
const createLocalizedText = (text: string): LocalizableText => ({
  fallback: text
})

// Sample legal questionnaires
const getSampleLegalQuestionnaires = (): Questionnaire[] => [
  // Immigration Case Questionnaire
  {
    version: '1.0.0',
    id: 'immigration_case_2024',
    name: createLocalizedText('Immigration Case Intake Form'),
    description: createLocalizedText('Comprehensive intake form for immigration cases including visa applications, family petitions, and citizenship matters'),
    pages: [
      {
        id: 'page_client_info',
        title: createLocalizedText('Client Information'),
        sections: [
          {
            id: 'section_personal',
            title: createLocalizedText('Personal Details'),
            groups: [
              {
                id: 'group_basic_info',
                title: createLocalizedText('Basic Information'),
                items: [
                  {
                    id: 'full_name',
                    type: 'text',
                    props: { placeholder: 'Enter your full legal name' },
                    label: createLocalizedText('Full Legal Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'date_of_birth',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Birth'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'place_of_birth',
                    type: 'text',
                    props: { placeholder: 'City, State/Province, Country' },
                    label: createLocalizedText('Place of Birth'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'nationality',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Please select...', value: '' },
                        { label: 'United States', value: 'us' },
                        { label: 'Canada', value: 'ca' },
                        { label: 'Mexico', value: 'mx' },
                        { label: 'United Kingdom', value: 'uk' },
                        { label: 'Germany', value: 'de' },
                        { label: 'France', value: 'fr' },
                        { label: 'China', value: 'cn' },
                        { label: 'India', value: 'in' },
                        { label: 'Other', value: 'other' }
                      ]
                    },
                    label: createLocalizedText('Current Nationality/Citizenship'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_contact',
            title: createLocalizedText('Contact Information'),
            groups: [
              {
                id: 'group_contact',
                title: createLocalizedText('Contact Details'),
                items: [
                  {
                    id: 'email',
                    type: 'email',
                    props: { placeholder: 'your.email@example.com' },
                    label: createLocalizedText('Email Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'phone',
                    type: 'tel',
                    props: { placeholder: '(555) 123-4567' },
                    label: createLocalizedText('Phone Number'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'current_address',
                    type: 'textarea',
                    props: { placeholder: 'Street address, City, State, ZIP Code, Country', rows: 3 },
                    label: createLocalizedText('Current Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'mailing_address',
                    type: 'textarea',
                    props: { placeholder: 'If different from current address', rows: 3 },
                    label: createLocalizedText('Mailing Address (if different)'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_immigration_history',
        title: createLocalizedText('Immigration History & Status'),
        sections: [
          {
            id: 'section_current_status',
            title: createLocalizedText('Current Immigration Status'),
            groups: [
              {
                id: 'group_status',
                title: createLocalizedText('Status Information'),
                items: [
                  {
                    id: 'current_status',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Please select...', value: '' },
                        { label: 'U.S. Citizen', value: 'citizen' },
                        { label: 'Lawful Permanent Resident (Green Card)', value: 'lpr' },
                        { label: 'H-1B Visa Holder', value: 'h1b' },
                        { label: 'F-1 Student Visa', value: 'f1' },
                        { label: 'L-1 Visa', value: 'l1' },
                        { label: 'Tourist/Visitor (B-1/B-2)', value: 'b1b2' },
                        { label: 'Asylum/Refugee Status', value: 'asylum' },
                        { label: 'Undocumented', value: 'undocumented' },
                        { label: 'Other', value: 'other' }
                      ]
                    },
                    label: createLocalizedText('Current Immigration Status'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'status_expiration',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Status Expiration Date (if applicable)'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'entry_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Last Entry to US'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'entry_location',
                    type: 'text',
                    props: { placeholder: 'Port of entry (airport, border crossing, etc.)' },
                    label: createLocalizedText('Location of Last Entry'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_family',
            title: createLocalizedText('Family Information'),
            groups: [
              {
                id: 'group_spouse',
                title: createLocalizedText('Spouse Information'),
                items: [
                  {
                    id: 'marital_status',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Single', value: 'single' },
                        { label: 'Married', value: 'married' },
                        { label: 'Divorced', value: 'divorced' },
                        { label: 'Widowed', value: 'widowed' }
                      ]
                    },
                    label: createLocalizedText('Marital Status'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'spouse_name',
                    type: 'text',
                    props: { placeholder: 'Full legal name of spouse' },
                    label: createLocalizedText('Spouse Full Name'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'spouse_citizenship',
                    type: 'text',
                    props: { placeholder: 'Country of citizenship' },
                    label: createLocalizedText('Spouse Citizenship'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'children_count',
                    type: 'number',
                    props: { min: 0, max: 20 },
                    label: createLocalizedText('Number of Children'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_case_details',
        title: createLocalizedText('Case Details & Goals'),
        sections: [
          {
            id: 'section_case_type',
            title: createLocalizedText('Type of Immigration Matter'),
            groups: [
              {
                id: 'group_case_type',
                title: createLocalizedText('Case Information'),
                items: [
                  {
                    id: 'case_type',
                    type: 'checkbox',
                    props: {
                      options: [
                        { label: 'Family-based petition (I-130)', value: 'family_petition' },
                        { label: 'Adjustment of status (I-485)', value: 'adjustment' },
                        { label: 'Naturalization/Citizenship (N-400)', value: 'citizenship' },
                        { label: 'Work visa (H-1B, L-1, etc.)', value: 'work_visa' },
                        { label: 'Student visa matters', value: 'student' },
                        { label: 'Asylum/Refugee case', value: 'asylum' },
                        { label: 'Deportation defense', value: 'deportation' },
                        { label: 'DACA renewal', value: 'daca' },
                        { label: 'Other', value: 'other' }
                      ]
                    },
                    label: createLocalizedText('Type of Immigration Case (select all that apply)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'case_urgency',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Please select...', value: '' },
                        { label: 'Emergency - Immediate attention needed', value: 'emergency' },
                        { label: 'Urgent - Within 30 days', value: 'urgent' },
                        { label: 'Standard - Within 3 months', value: 'standard' },
                        { label: 'No rush - Flexible timeline', value: 'flexible' }
                      ]
                    },
                    label: createLocalizedText('Case Urgency'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'case_description',
                    type: 'textarea',
                    props: { placeholder: 'Please describe your immigration goals and any specific circumstances...', rows: 5 },
                    label: createLocalizedText('Detailed Case Description'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'previous_attorney',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Have you worked with an immigration attorney before?'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  },

  // Accident Case Questionnaire
  {
    version: '1.0.0',
    id: 'accident_case_2024',
    name: createLocalizedText('Personal Injury Case Intake Form'),
    description: createLocalizedText('Comprehensive intake form for personal injury cases including auto accidents, slip and fall, and workplace injuries'),
    pages: [
      {
        id: 'page_client_basic',
        title: createLocalizedText('Client Information'),
        sections: [
          {
            id: 'section_client_details',
            title: createLocalizedText('Personal Information'),
            groups: [
              {
                id: 'group_client_basic',
                title: createLocalizedText('Basic Details'),
                items: [
                  {
                    id: 'client_name',
                    type: 'text',
                    props: { placeholder: 'Enter your full legal name' },
                    label: createLocalizedText('Full Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'client_dob',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Birth'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'client_ssn',
                    type: 'text',
                    props: { placeholder: 'XXX-XX-XXXX' },
                    label: createLocalizedText('Social Security Number'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'client_email',
                    type: 'email',
                    props: { placeholder: 'your.email@example.com' },
                    label: createLocalizedText('Email Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'client_phone',
                    type: 'tel',
                    props: { placeholder: '(555) 123-4567' },
                    label: createLocalizedText('Phone Number'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'client_address',
                    type: 'textarea',
                    props: { placeholder: 'Street address, City, State, ZIP Code', rows: 3 },
                    label: createLocalizedText('Home Address'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_incident_details',
        title: createLocalizedText('Incident Details'),
        sections: [
          {
            id: 'section_accident_info',
            title: createLocalizedText('Accident Information'),
            groups: [
              {
                id: 'group_accident_basic',
                title: createLocalizedText('Basic Accident Details'),
                items: [
                  {
                    id: 'accident_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Accident'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'accident_time',
                    type: 'time',
                    props: {},
                    label: createLocalizedText('Time of Accident (if known)'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'accident_type',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Please select...', value: '' },
                        { label: 'Motor Vehicle Accident', value: 'auto' },
                        { label: 'Slip and Fall', value: 'slip_fall' },
                        { label: 'Workplace Injury', value: 'workplace' },
                        { label: 'Medical Malpractice', value: 'medical' },
                        { label: 'Product Liability', value: 'product' },
                        { label: 'Dog Bite', value: 'dog_bite' },
                        { label: 'Premises Liability', value: 'premises' },
                        { label: 'Other', value: 'other' }
                      ]
                    },
                    label: createLocalizedText('Type of Accident'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'accident_location',
                    type: 'textarea',
                    props: { placeholder: 'Exact location where accident occurred (address, intersection, etc.)', rows: 3 },
                    label: createLocalizedText('Location of Accident'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'accident_description',
                    type: 'textarea',
                    props: { placeholder: 'Please describe what happened in detail...', rows: 6 },
                    label: createLocalizedText('Detailed Description of Accident'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'weather_conditions',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Clear/Sunny', value: 'clear' },
                        { label: 'Rainy', value: 'rainy' },
                        { label: 'Snow/Ice', value: 'snow' },
                        { label: 'Foggy', value: 'foggy' },
                        { label: 'Dark/Night', value: 'dark' },
                        { label: "Don't remember", value: 'unknown' }
                      ]
                    },
                    label: createLocalizedText('Weather/Lighting Conditions'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_injuries_treatment',
        title: createLocalizedText('Injuries & Medical Treatment'),
        sections: [
          {
            id: 'section_injuries',
            title: createLocalizedText('Injury Information'),
            groups: [
              {
                id: 'group_injuries',
                title: createLocalizedText('Injuries Sustained'),
                items: [
                  {
                    id: 'injury_types',
                    type: 'checkbox',
                    props: {
                      options: [
                        { label: 'Head/Brain injury', value: 'head' },
                        { label: 'Neck injury', value: 'neck' },
                        { label: 'Back/Spine injury', value: 'back' },
                        { label: 'Broken bones/Fractures', value: 'fractures' },
                        { label: 'Cuts/Lacerations', value: 'cuts' },
                        { label: 'Bruises/Contusions', value: 'bruises' },
                        { label: 'Soft tissue injuries', value: 'soft_tissue' },
                        { label: 'Internal injuries', value: 'internal' },
                        { label: 'Burns', value: 'burns' },
                        { label: 'Other', value: 'other' }
                      ]
                    },
                    label: createLocalizedText('Type of Injuries (select all that apply)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'injury_severity',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Minor - No hospitalization required', value: 'minor' },
                        { label: 'Moderate - Required emergency room visit', value: 'moderate' },
                        { label: 'Severe - Required hospitalization', value: 'severe' },
                        { label: 'Critical - Life-threatening injuries', value: 'critical' }
                      ]
                    },
                    label: createLocalizedText('Severity of Injuries'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'medical_treatment',
                    type: 'textarea',
                    props: { placeholder: 'List all medical treatment received (hospitals, doctors, physical therapy, etc.)', rows: 4 },
                    label: createLocalizedText('Medical Treatment Received'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'ongoing_treatment',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Are you still receiving medical treatment?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'medical_expenses',
                    type: 'text',
                    props: { placeholder: 'Estimated total amount' },
                    label: createLocalizedText('Estimated Medical Expenses to Date'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_insurance_liability',
        title: createLocalizedText('Insurance & Liability'),
        sections: [
          {
            id: 'section_insurance',
            title: createLocalizedText('Insurance Information'),
            groups: [
              {
                id: 'group_insurance',
                title: createLocalizedText('Insurance Details'),
                items: [
                  {
                    id: 'has_insurance',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Do you have health insurance?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'insurance_company',
                    type: 'text',
                    props: { placeholder: 'Name of insurance company' },
                    label: createLocalizedText('Health Insurance Company'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'other_party_insured',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' },
                        { label: "Don't know", value: 'unknown' }
                      ]
                    },
                    label: createLocalizedText('Was the other party insured?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'police_report',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' },
                        { label: "Don't know", value: 'unknown' }
                      ]
                    },
                    label: createLocalizedText('Was a police report filed?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'witnesses',
                    type: 'textarea',
                    props: { placeholder: 'Names and contact information of any witnesses', rows: 3 },
                    label: createLocalizedText('Witness Information'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  },

  // Defamation Case Questionnaire
  {
    version: '1.0.0',
    id: 'defamation_case_2024',
    name: createLocalizedText('Defamation Case Intake Form'),
    description: createLocalizedText('Comprehensive intake form for defamation, libel, and slander cases including online reputation matters'),
    pages: [
      {
        id: 'page_plaintiff_info',
        title: createLocalizedText('Plaintiff Information'),
        sections: [
          {
            id: 'section_plaintiff_details',
            title: createLocalizedText('Personal/Business Information'),
            groups: [
              {
                id: 'group_plaintiff_basic',
                title: createLocalizedText('Basic Information'),
                items: [
                  {
                    id: 'plaintiff_type',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Individual', value: 'individual' },
                        { label: 'Business/Corporation', value: 'business' },
                        { label: 'Non-profit organization', value: 'nonprofit' }
                      ]
                    },
                    label: createLocalizedText('Type of Plaintiff'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'plaintiff_name',
                    type: 'text',
                    props: { placeholder: 'Full name or business name' },
                    label: createLocalizedText('Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'plaintiff_title',
                    type: 'text',
                    props: { placeholder: 'Professional title or position' },
                    label: createLocalizedText('Title/Position (if applicable)'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'plaintiff_email',
                    type: 'email',
                    props: { placeholder: 'your.email@example.com' },
                    label: createLocalizedText('Email Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'plaintiff_phone',
                    type: 'tel',
                    props: { placeholder: '(555) 123-4567' },
                    label: createLocalizedText('Phone Number'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'plaintiff_address',
                    type: 'textarea',
                    props: { placeholder: 'Business or home address', rows: 3 },
                    label: createLocalizedText('Address'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_defamatory_statements',
        title: createLocalizedText('Defamatory Statements'),
        sections: [
          {
            id: 'section_statements',
            title: createLocalizedText('Statement Details'),
            groups: [
              {
                id: 'group_statements',
                title: createLocalizedText('The Defamatory Content'),
                items: [
                  {
                    id: 'statement_type',
                    type: 'checkbox',
                    props: {
                      options: [
                        { label: 'Written statement (libel)', value: 'libel' },
                        { label: 'Spoken statement (slander)', value: 'slander' },
                        { label: 'Social media post', value: 'social_media' },
                        { label: 'Online review', value: 'review' },
                        { label: 'News article/blog post', value: 'article' },
                        { label: 'Video/audio recording', value: 'media' },
                        { label: 'Email or message', value: 'email' },
                        { label: 'Other', value: 'other' }
                      ]
                    },
                    label: createLocalizedText('Type of Defamatory Statement (select all that apply)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'statement_content',
                    type: 'textarea',
                    props: { placeholder: 'Provide the exact text of the defamatory statements or describe what was said...', rows: 6 },
                    label: createLocalizedText('Exact Content of Defamatory Statements'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'statement_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date Statement was Made/Published'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'statement_location',
                    type: 'text',
                    props: { placeholder: 'Website, platform, location where statement was made' },
                    label: createLocalizedText('Where Statement was Made/Published'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'statement_false',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Completely false', value: 'completely_false' },
                        { label: 'Partially false', value: 'partially_false' },
                        { label: 'Misleading context', value: 'misleading' }
                      ]
                    },
                    label: createLocalizedText('How is the statement false?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'truth_explanation',
                    type: 'textarea',
                    props: { placeholder: 'Explain what the truth is and how you can prove it...', rows: 4 },
                    label: createLocalizedText('What is the truth? How can you prove it?'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_defendant_damages',
        title: createLocalizedText('Defendant & Damages'),
        sections: [
          {
            id: 'section_defendant',
            title: createLocalizedText('Defendant Information'),
            groups: [
              {
                id: 'group_defendant',
                title: createLocalizedText('Person/Entity Who Made Statement'),
                items: [
                  {
                    id: 'defendant_name',
                    type: 'text',
                    props: { placeholder: 'Name of person or organization' },
                    label: createLocalizedText('Defendant Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'defendant_relationship',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Please select...', value: '' },
                        { label: 'Former employee', value: 'former_employee' },
                        { label: 'Competitor', value: 'competitor' },
                        { label: 'Customer/client', value: 'customer' },
                        { label: 'Business partner', value: 'partner' },
                        { label: 'Family member', value: 'family' },
                        { label: 'Friend/acquaintance', value: 'friend' },
                        { label: 'Stranger', value: 'stranger' },
                        { label: 'News organization', value: 'media' },
                        { label: 'Online reviewer', value: 'reviewer' },
                        { label: 'Other', value: 'other' }
                      ]
                    },
                    label: createLocalizedText('Relationship to Defendant'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'defendant_contact',
                    type: 'textarea',
                    props: { placeholder: 'Any known contact information for defendant', rows: 2 },
                    label: createLocalizedText('Defendant Contact Information (if known)'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_damages',
            title: createLocalizedText('Damages and Impact'),
            groups: [
              {
                id: 'group_damages',
                title: createLocalizedText('Harm Caused'),
                items: [
                  {
                    id: 'damage_types',
                    type: 'checkbox',
                    props: {
                      options: [
                        { label: 'Loss of business/income', value: 'business_loss' },
                        { label: 'Damage to professional reputation', value: 'professional_damage' },
                        { label: 'Personal humiliation/embarrassment', value: 'personal_damage' },
                        { label: 'Loss of job opportunities', value: 'job_loss' },
                        { label: 'Emotional distress', value: 'emotional_distress' },
                        { label: 'Loss of relationships', value: 'relationship_loss' },
                        { label: 'Medical expenses (therapy, etc.)', value: 'medical_expenses' },
                        { label: 'Other financial losses', value: 'other_financial' }
                      ]
                    },
                    label: createLocalizedText('Types of Damages Suffered (select all that apply)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'financial_impact',
                    type: 'text',
                    props: { placeholder: 'Estimated dollar amount of losses' },
                    label: createLocalizedText('Estimated Financial Impact'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'damage_evidence',
                    type: 'textarea',
                    props: { placeholder: 'Describe evidence of damages (lost contracts, decreased sales, etc.)', rows: 4 },
                    label: createLocalizedText('Evidence of Damages'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'public_figure',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' },
                        { label: 'Unsure', value: 'unsure' }
                      ]
                    },
                    label: createLocalizedText('Are you considered a public figure?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'correction_requested',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Have you requested a correction or retraction?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'desired_outcome',
                    type: 'checkbox',
                    props: {
                      options: [
                        { label: 'Monetary damages', value: 'monetary' },
                        { label: 'Public retraction/apology', value: 'retraction' },
                        { label: 'Removal of defamatory content', value: 'removal' },
                        { label: 'Injunction to prevent future statements', value: 'injunction' },
                        { label: 'Repair of reputation', value: 'reputation_repair' }
                      ]
                    },
                    label: createLocalizedText('Desired Outcome (select all that apply)'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  },

  // Marriage Prenup Intake Questionnaire
  {
    version: '1.0.0',
    id: 'prenup_intake_2024',
    name: createLocalizedText('Marriage Prenuptial Agreement Intake Form'),
    description: createLocalizedText('Comprehensive intake form for prenuptial agreements covering assets, debts, and marital expectations'),
    pages: [
      {
        id: 'page_couple_info',
        title: createLocalizedText('Couple Information'),
        sections: [
          {
            id: 'section_bride_info',
            title: createLocalizedText('Bride Information'),
            groups: [
              {
                id: 'group_bride_details',
                title: createLocalizedText('Bride Personal Details'),
                items: [
                  {
                    id: 'bride_full_name',
                    type: 'text',
                    props: { placeholder: 'Enter full legal name' },
                    label: createLocalizedText('Bride Full Legal Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'bride_dob',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Birth'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'bride_occupation',
                    type: 'text',
                    props: { placeholder: 'Current occupation/profession' },
                    label: createLocalizedText('Occupation'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'bride_email',
                    type: 'email',
                    props: { placeholder: 'bride.email@example.com' },
                    label: createLocalizedText('Email Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'bride_phone',
                    type: 'tel',
                    props: { placeholder: '(555) 123-4567' },
                    label: createLocalizedText('Phone Number'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'bride_address',
                    type: 'textarea',
                    props: { placeholder: 'Current address', rows: 3 },
                    label: createLocalizedText('Current Address'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_groom_info',
            title: createLocalizedText('Groom Information'),
            groups: [
              {
                id: 'group_groom_details',
                title: createLocalizedText('Groom Personal Details'),
                items: [
                  {
                    id: 'groom_full_name',
                    type: 'text',
                    props: { placeholder: 'Enter full legal name' },
                    label: createLocalizedText('Groom Full Legal Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'groom_dob',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Birth'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'groom_occupation',
                    type: 'text',
                    props: { placeholder: 'Current occupation/profession' },
                    label: createLocalizedText('Occupation'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'groom_email',
                    type: 'email',
                    props: { placeholder: 'groom.email@example.com' },
                    label: createLocalizedText('Email Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'groom_phone',
                    type: 'tel',
                    props: { placeholder: '(555) 123-4567' },
                    label: createLocalizedText('Phone Number'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'groom_address',
                    type: 'textarea',
                    props: { placeholder: 'Current address', rows: 3 },
                    label: createLocalizedText('Current Address'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_marriage_details',
        title: createLocalizedText('Marriage Plans'),
        sections: [
          {
            id: 'section_wedding',
            title: createLocalizedText('Wedding Information'),
            groups: [
              {
                id: 'group_wedding_details',
                title: createLocalizedText('Wedding Details'),
                items: [
                  {
                    id: 'wedding_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Planned Wedding Date'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'wedding_location',
                    type: 'text',
                    props: { placeholder: 'City, State where wedding will take place' },
                    label: createLocalizedText('Wedding Location'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'previous_marriages',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Neither party', value: 'neither' },
                        { label: 'Bride only', value: 'bride' },
                        { label: 'Groom only', value: 'groom' },
                        { label: 'Both parties', value: 'both' }
                      ]
                    },
                    label: createLocalizedText('Previous Marriages'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'children_existing',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Neither party has children', value: 'neither' },
                        { label: 'Bride has children', value: 'bride' },
                        { label: 'Groom has children', value: 'groom' },
                        { label: 'Both parties have children', value: 'both' }
                      ]
                    },
                    label: createLocalizedText('Existing Children'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_assets_debts',
        title: createLocalizedText('Assets & Debts'),
        sections: [
          {
            id: 'section_assets',
            title: createLocalizedText('Asset Information'),
            groups: [
              {
                id: 'group_assets',
                title: createLocalizedText('Pre-Marital Assets'),
                items: [
                  {
                    id: 'bride_assets',
                    type: 'textarea',
                    props: { placeholder: 'List major assets (real estate, investments, businesses, etc.)', rows: 4 },
                    label: createLocalizedText('Bride Pre-Marital Assets'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'bride_asset_value',
                    type: 'text',
                    props: { placeholder: 'Estimated total value' },
                    label: createLocalizedText('Bride Assets Estimated Value'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'groom_assets',
                    type: 'textarea',
                    props: { placeholder: 'List major assets (real estate, investments, businesses, etc.)', rows: 4 },
                    label: createLocalizedText('Groom Pre-Marital Assets'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'groom_asset_value',
                    type: 'text',
                    props: { placeholder: 'Estimated total value' },
                    label: createLocalizedText('Groom Assets Estimated Value'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'joint_assets_plan',
                    type: 'textarea',
                    props: { placeholder: 'How will marital property be handled?', rows: 3 },
                    label: createLocalizedText('Marital Property Arrangements'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_debts',
            title: createLocalizedText('Debt Information'),
            groups: [
              {
                id: 'group_debts',
                title: createLocalizedText('Pre-Marital Debts'),
                items: [
                  {
                    id: 'bride_debts',
                    type: 'textarea',
                    props: { placeholder: 'List debts (student loans, credit cards, mortgages, etc.)', rows: 4 },
                    label: createLocalizedText('Bride Pre-Marital Debts'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'groom_debts',
                    type: 'textarea',
                    props: { placeholder: 'List debts (student loans, credit cards, mortgages, etc.)', rows: 4 },
                    label: createLocalizedText('Groom Pre-Marital Debts'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'debt_responsibility',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Each party responsible for own pre-marital debts', value: 'separate' },
                        { label: 'All debts become joint responsibility', value: 'joint' },
                        { label: 'Mixed arrangement (specify)', value: 'mixed' }
                      ]
                    },
                    label: createLocalizedText('Debt Responsibility Arrangement'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  },

  // Testament/Will Intake Questionnaire
  {
    version: '1.0.0',
    id: 'testament_intake_2024',
    name: createLocalizedText('Last Will & Testament Intake Form'),
    description: createLocalizedText('Comprehensive intake form for will and estate planning including beneficiaries, assets, and final wishes'),
    pages: [
      {
        id: 'page_testator_info',
        title: createLocalizedText('Testator Information'),
        sections: [
          {
            id: 'section_personal_details',
            title: createLocalizedText('Personal Information'),
            groups: [
              {
                id: 'group_testator_basic',
                title: createLocalizedText('Basic Information'),
                items: [
                  {
                    id: 'testator_name',
                    type: 'text',
                    props: { placeholder: 'Enter full legal name' },
                    label: createLocalizedText('Full Legal Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'testator_dob',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Birth'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'testator_ssn',
                    type: 'text',
                    props: { placeholder: 'XXX-XX-XXXX' },
                    label: createLocalizedText('Social Security Number'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'testator_address',
                    type: 'textarea',
                    props: { placeholder: 'Current address', rows: 3 },
                    label: createLocalizedText('Current Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'marital_status_will',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Please select...', value: '' },
                        { label: 'Single', value: 'single' },
                        { label: 'Married', value: 'married' },
                        { label: 'Divorced', value: 'divorced' },
                        { label: 'Widowed', value: 'widowed' }
                      ]
                    },
                    label: createLocalizedText('Marital Status'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'spouse_name_will',
                    type: 'text',
                    props: { placeholder: 'Full name of spouse' },
                    label: createLocalizedText('Spouse Name (if married)'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_beneficiaries',
        title: createLocalizedText('Beneficiaries & Heirs'),
        sections: [
          {
            id: 'section_primary_beneficiaries',
            title: createLocalizedText('Primary Beneficiaries'),
            groups: [
              {
                id: 'group_beneficiaries',
                title: createLocalizedText('Beneficiary Information'),
                items: [
                  {
                    id: 'children_info',
                    type: 'textarea',
                    props: { placeholder: 'List all children (names, ages, addresses)', rows: 4 },
                    label: createLocalizedText('Children Information'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'other_beneficiaries',
                    type: 'textarea',
                    props: { placeholder: 'List other beneficiaries (relatives, friends, charities)', rows: 4 },
                    label: createLocalizedText('Other Beneficiaries'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'guardian_minors',
                    type: 'text',
                    props: { placeholder: 'Name of proposed guardian for minor children' },
                    label: createLocalizedText('Guardian for Minor Children'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'executor_primary',
                    type: 'text',
                    props: { placeholder: 'Name of person to execute the will' },
                    label: createLocalizedText('Primary Executor'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'executor_alternate',
                    type: 'text',
                    props: { placeholder: 'Backup executor if primary unavailable' },
                    label: createLocalizedText('Alternate Executor'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_assets_distribution',
        title: createLocalizedText('Assets & Distribution'),
        sections: [
          {
            id: 'section_estate_assets',
            title: createLocalizedText('Estate Assets'),
            groups: [
              {
                id: 'group_estate_details',
                title: createLocalizedText('Asset Distribution'),
                items: [
                  {
                    id: 'real_estate_will',
                    type: 'textarea',
                    props: { placeholder: 'List all real estate properties and how they should be distributed', rows: 4 },
                    label: createLocalizedText('Real Estate Distribution'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'personal_property',
                    type: 'textarea',
                    props: { placeholder: 'Jewelry, vehicles, furniture, collectibles, etc.', rows: 4 },
                    label: createLocalizedText('Personal Property Distribution'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'financial_accounts',
                    type: 'textarea',
                    props: { placeholder: 'Bank accounts, investments, retirement accounts', rows: 4 },
                    label: createLocalizedText('Financial Accounts Distribution'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'business_interests',
                    type: 'textarea',
                    props: { placeholder: 'Business ownership, partnerships, etc.', rows: 3 },
                    label: createLocalizedText('Business Interests'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'charitable_bequests',
                    type: 'textarea',
                    props: { placeholder: 'Any donations to charities or organizations', rows: 3 },
                    label: createLocalizedText('Charitable Bequests'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'residuary_estate',
                    type: 'text',
                    props: { placeholder: 'Who receives remainder of estate after specific bequests' },
                    label: createLocalizedText('Residuary Estate Beneficiary'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  },

  // Sale of Land Questionnaire
  {
    version: '1.0.0',
    id: 'land_sale_2024',
    name: createLocalizedText('Real Estate Sale Transaction Form'),
    description: createLocalizedText('Comprehensive form for land and real estate sale transactions including property details and terms'),
    pages: [
      {
        id: 'page_property_details',
        title: createLocalizedText('Property Information'),
        sections: [
          {
            id: 'section_property_info',
            title: createLocalizedText('Property Details'),
            groups: [
              {
                id: 'group_property_basic',
                title: createLocalizedText('Basic Property Information'),
                items: [
                  {
                    id: 'property_address',
                    type: 'textarea',
                    props: { placeholder: 'Full property address including city, state, ZIP', rows: 3 },
                    label: createLocalizedText('Property Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'legal_description',
                    type: 'textarea',
                    props: { placeholder: 'Legal description from deed or survey', rows: 4 },
                    label: createLocalizedText('Legal Description'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'parcel_number',
                    type: 'text',
                    props: { placeholder: 'Tax parcel/PIN number' },
                    label: createLocalizedText('Tax Parcel Number'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'property_type',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Please select...', value: '' },
                        { label: 'Vacant Land', value: 'vacant' },
                        { label: 'Residential Property', value: 'residential' },
                        { label: 'Commercial Property', value: 'commercial' },
                        { label: 'Industrial Property', value: 'industrial' },
                        { label: 'Agricultural Land', value: 'agricultural' },
                        { label: 'Mixed Use', value: 'mixed' }
                      ]
                    },
                    label: createLocalizedText('Property Type'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'acreage_size',
                    type: 'text',
                    props: { placeholder: 'Size in acres or square feet' },
                    label: createLocalizedText('Property Size'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'zoning',
                    type: 'text',
                    props: { placeholder: 'Current zoning classification' },
                    label: createLocalizedText('Zoning'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_seller_buyer',
        title: createLocalizedText('Parties Information'),
        sections: [
          {
            id: 'section_seller_info',
            title: createLocalizedText('Seller Information'),
            groups: [
              {
                id: 'group_seller_details',
                title: createLocalizedText('Seller Details'),
                items: [
                  {
                    id: 'seller_name',
                    type: 'text',
                    props: { placeholder: 'Full legal name or entity name' },
                    label: createLocalizedText('Seller Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'seller_type',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Individual', value: 'individual' },
                        { label: 'Corporation', value: 'corporation' },
                        { label: 'LLC', value: 'llc' },
                        { label: 'Partnership', value: 'partnership' },
                        { label: 'Trust', value: 'trust' },
                        { label: 'Estate', value: 'estate' }
                      ]
                    },
                    label: createLocalizedText('Seller Type'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'seller_address',
                    type: 'textarea',
                    props: { placeholder: 'Seller current address', rows: 3 },
                    label: createLocalizedText('Seller Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'seller_phone',
                    type: 'tel',
                    props: { placeholder: '(555) 123-4567' },
                    label: createLocalizedText('Seller Phone'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_buyer_info',
            title: createLocalizedText('Buyer Information'),
            groups: [
              {
                id: 'group_buyer_details',
                title: createLocalizedText('Buyer Details'),
                items: [
                  {
                    id: 'buyer_name',
                    type: 'text',
                    props: { placeholder: 'Full legal name or entity name' },
                    label: createLocalizedText('Buyer Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'buyer_type',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Individual', value: 'individual' },
                        { label: 'Corporation', value: 'corporation' },
                        { label: 'LLC', value: 'llc' },
                        { label: 'Partnership', value: 'partnership' },
                        { label: 'Trust', value: 'trust' }
                      ]
                    },
                    label: createLocalizedText('Buyer Type'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'buyer_address',
                    type: 'textarea',
                    props: { placeholder: 'Buyer current address', rows: 3 },
                    label: createLocalizedText('Buyer Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'buyer_phone',
                    type: 'tel',
                    props: { placeholder: '(555) 123-4567' },
                    label: createLocalizedText('Buyer Phone'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_transaction_terms',
        title: createLocalizedText('Transaction Terms'),
        sections: [
          {
            id: 'section_financial_terms',
            title: createLocalizedText('Financial Terms'),
            groups: [
              {
                id: 'group_financial_details',
                title: createLocalizedText('Purchase Terms'),
                items: [
                  {
                    id: 'sale_price',
                    type: 'text',
                    props: { placeholder: 'Total purchase price' },
                    label: createLocalizedText('Sale Price'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'earnest_money',
                    type: 'text',
                    props: { placeholder: 'Earnest money deposit amount' },
                    label: createLocalizedText('Earnest Money Deposit'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'financing_type',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Cash purchase', value: 'cash' },
                        { label: 'Conventional mortgage', value: 'conventional' },
                        { label: 'FHA loan', value: 'fha' },
                        { label: 'VA loan', value: 'va' },
                        { label: 'Owner financing', value: 'owner_financing' },
                        { label: 'Other', value: 'other' }
                      ]
                    },
                    label: createLocalizedText('Financing Type'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'closing_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Proposed Closing Date'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'possession_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Possession Date'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'special_conditions',
                    type: 'textarea',
                    props: { placeholder: 'Any special conditions or contingencies', rows: 4 },
                    label: createLocalizedText('Special Conditions'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  },

  // Sale of Car Questionnaire
  {
    version: '1.0.0',
    id: 'car_sale_2024',
    name: createLocalizedText('Vehicle Sale Transaction Form'),
    description: createLocalizedText('Comprehensive form for vehicle sale transactions including vehicle details, condition, and transfer requirements'),
    pages: [
      {
        id: 'page_vehicle_info',
        title: createLocalizedText('Vehicle Information'),
        sections: [
          {
            id: 'section_vehicle_details',
            title: createLocalizedText('Vehicle Details'),
            groups: [
              {
                id: 'group_vehicle_basic',
                title: createLocalizedText('Basic Vehicle Information'),
                items: [
                  {
                    id: 'vehicle_year',
                    type: 'number',
                    props: { min: 1900, max: 2030 },
                    label: createLocalizedText('Year'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_make',
                    type: 'text',
                    props: { placeholder: 'Toyota, Ford, BMW, etc.' },
                    label: createLocalizedText('Make'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_model',
                    type: 'text',
                    props: { placeholder: 'Camry, F-150, 3 Series, etc.' },
                    label: createLocalizedText('Model'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_trim',
                    type: 'text',
                    props: { placeholder: 'LE, XLT, Sport, etc.' },
                    label: createLocalizedText('Trim Level'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'vehicle_vin',
                    type: 'text',
                    props: { placeholder: '17-character VIN number' },
                    label: createLocalizedText('VIN Number'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_mileage',
                    type: 'number',
                    props: { min: 0 },
                    label: createLocalizedText('Current Mileage'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_color',
                    type: 'text',
                    props: { placeholder: 'Exterior color' },
                    label: createLocalizedText('Color'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'license_plate',
                    type: 'text',
                    props: { placeholder: 'Current license plate number' },
                    label: createLocalizedText('License Plate Number'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_vehicle_condition',
            title: createLocalizedText('Vehicle Condition'),
            groups: [
              {
                id: 'group_condition_details',
                title: createLocalizedText('Condition Assessment'),
                items: [
                  {
                    id: 'overall_condition',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Excellent', value: 'excellent' },
                        { label: 'Very Good', value: 'very_good' },
                        { label: 'Good', value: 'good' },
                        { label: 'Fair', value: 'fair' },
                        { label: 'Poor', value: 'poor' }
                      ]
                    },
                    label: createLocalizedText('Overall Condition'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'mechanical_issues',
                    type: 'textarea',
                    props: { placeholder: 'Describe any known mechanical problems', rows: 3 },
                    label: createLocalizedText('Known Mechanical Issues'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'accident_history',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'No accidents', value: 'no' },
                        { label: 'Minor accidents/damage', value: 'minor' },
                        { label: 'Major accidents/damage', value: 'major' }
                      ]
                    },
                    label: createLocalizedText('Accident History'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'maintenance_records',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Complete records available', value: 'complete' },
                        { label: 'Partial records available', value: 'partial' },
                        { label: 'No records available', value: 'none' }
                      ]
                    },
                    label: createLocalizedText('Maintenance Records'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_seller_buyer_vehicle',
        title: createLocalizedText('Seller & Buyer Information'),
        sections: [
          {
            id: 'section_vehicle_seller',
            title: createLocalizedText('Seller Information'),
            groups: [
              {
                id: 'group_vehicle_seller_details',
                title: createLocalizedText('Seller Details'),
                items: [
                  {
                    id: 'vehicle_seller_name',
                    type: 'text',
                    props: { placeholder: 'Full legal name' },
                    label: createLocalizedText('Seller Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_seller_address',
                    type: 'textarea',
                    props: { placeholder: 'Current address', rows: 3 },
                    label: createLocalizedText('Seller Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_seller_phone',
                    type: 'tel',
                    props: { placeholder: '(555) 123-4567' },
                    label: createLocalizedText('Seller Phone'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_seller_email',
                    type: 'email',
                    props: { placeholder: 'seller@example.com' },
                    label: createLocalizedText('Seller Email'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'drivers_license_seller',
                    type: 'text',
                    props: { placeholder: 'Driver license number' },
                    label: createLocalizedText('Seller Driver License #'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_vehicle_buyer',
            title: createLocalizedText('Buyer Information'),
            groups: [
              {
                id: 'group_vehicle_buyer_details',
                title: createLocalizedText('Buyer Details'),
                items: [
                  {
                    id: 'vehicle_buyer_name',
                    type: 'text',
                    props: { placeholder: 'Full legal name' },
                    label: createLocalizedText('Buyer Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_buyer_address',
                    type: 'textarea',
                    props: { placeholder: 'Current address', rows: 3 },
                    label: createLocalizedText('Buyer Address'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_buyer_phone',
                    type: 'tel',
                    props: { placeholder: '(555) 123-4567' },
                    label: createLocalizedText('Buyer Phone'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_buyer_email',
                    type: 'email',
                    props: { placeholder: 'buyer@example.com' },
                    label: createLocalizedText('Buyer Email'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'drivers_license_buyer',
                    type: 'text',
                    props: { placeholder: 'Driver license number' },
                    label: createLocalizedText('Buyer Driver License #'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_sale_terms',
        title: createLocalizedText('Sale Terms'),
        sections: [
          {
            id: 'section_vehicle_financial',
            title: createLocalizedText('Financial Terms'),
            groups: [
              {
                id: 'group_vehicle_financial_details',
                title: createLocalizedText('Purchase Details'),
                items: [
                  {
                    id: 'vehicle_sale_price',
                    type: 'text',
                    props: { placeholder: 'Total sale price' },
                    label: createLocalizedText('Sale Price'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'payment_method',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Cash', value: 'cash' },
                        { label: 'Cashier Check', value: 'cashier_check' },
                        { label: 'Personal Check', value: 'personal_check' },
                        { label: 'Bank Transfer', value: 'bank_transfer' },
                        { label: 'Financing', value: 'financing' },
                        { label: 'Trade-in + Cash', value: 'trade_cash' }
                      ]
                    },
                    label: createLocalizedText('Payment Method'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'vehicle_sale_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Sale Date'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'warranty_info',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Sold as-is (no warranty)', value: 'as_is' },
                        { label: 'Limited warranty included', value: 'limited' },
                        { label: 'Extended warranty available', value: 'extended' }
                      ]
                    },
                    label: createLocalizedText('Warranty Status'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'title_status',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Clear title in hand', value: 'clear' },
                        { label: 'Lien to be satisfied at closing', value: 'lien' },
                        { label: 'Title being ordered', value: 'ordering' },
                        { label: 'Other (specify)', value: 'other' }
                      ]
                    },
                    label: createLocalizedText('Title Status'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'additional_terms',
                    type: 'textarea',
                    props: { placeholder: 'Any additional terms or conditions', rows: 3 },
                    label: createLocalizedText('Additional Terms'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  },

  // I-130 Petition for Alien Relative Questionnaire
  {
    version: '1.0.0',
    id: 'i130_petition_2024',
    name: createLocalizedText('I-130 Petition for Alien Relative'),
    description: createLocalizedText('Complete questionnaire for USCIS Form I-130 - Petition for Alien Relative. This form is used by U.S. citizens and permanent residents to petition for certain family members to immigrate to the United States.'),
    pages: [
      {
        id: 'page_petitioner_info',
        title: createLocalizedText('Part 1: Petitioner Information'),
        sections: [
          {
            id: 'section_petitioner_basic',
            title: createLocalizedText('Basic Information About Petitioner'),
            groups: [
              {
                id: 'group_petitioner_name',
                title: createLocalizedText('Full Name'),
                items: [
                  {
                    id: 'petitioner_family_name',
                    type: 'text',
                    props: { placeholder: 'Last name as shown on identity documents' },
                    label: createLocalizedText('Family Name (Last Name)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'petitioner_given_name',
                    type: 'text',
                    props: { placeholder: 'First name as shown on identity documents' },
                    label: createLocalizedText('Given Name (First Name)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'petitioner_middle_name',
                    type: 'text',
                    props: { placeholder: 'Middle name if applicable' },
                    label: createLocalizedText('Middle Name (if applicable)'),
                    required: false,
                    validation: []
                  }
                ]
              },
              {
                id: 'group_petitioner_other_names',
                title: createLocalizedText('Other Names Used'),
                items: [
                  {
                    id: 'petitioner_other_names_used',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Have you ever used other names (including maiden name, aliases, etc.)?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'petitioner_other_names_list',
                    type: 'textarea',
                    props: { placeholder: 'List all other names used, separated by commas', rows: 3 },
                    label: createLocalizedText('If yes, list all other names used:'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_petitioner_personal',
            title: createLocalizedText('Personal Details'),
            groups: [
              {
                id: 'group_petitioner_birth',
                title: createLocalizedText('Birth Information'),
                items: [
                  {
                    id: 'petitioner_dob',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Birth (mm/dd/yyyy)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'petitioner_country_birth',
                    type: 'text',
                    props: { placeholder: 'Country where you were born' },
                    label: createLocalizedText('Country of Birth'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'petitioner_city_birth',
                    type: 'text',
                    props: { placeholder: 'City where you were born' },
                    label: createLocalizedText('City of Birth'),
                    required: true,
                    validation: []
                  }
                ]
              },
              {
                id: 'group_petitioner_citizenship',
                title: createLocalizedText('Citizenship Status'),
                items: [
                  {
                    id: 'petitioner_citizenship_status',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'U.S. Citizen', value: 'citizen' },
                        { label: 'Lawful Permanent Resident', value: 'lpr' }
                      ]
                    },
                    label: createLocalizedText('What is your citizenship/immigration status?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'petitioner_citizenship_acquired',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Birth in the United States', value: 'birth_us' },
                        { label: 'Naturalization', value: 'naturalization' },
                        { label: 'Parents', value: 'parents' }
                      ]
                    },
                    label: createLocalizedText('If U.S. citizen, how did you acquire citizenship?'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'petitioner_alien_number',
                    type: 'text',
                    props: { placeholder: 'A-Number (if applicable)' },
                    label: createLocalizedText('USCIS Online Account Number or A-Number (if any)'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_petitioner_contact',
            title: createLocalizedText('Contact Information'),
            groups: [
              {
                id: 'group_petitioner_address',
                title: createLocalizedText('Physical Address'),
                items: [
                  {
                    id: 'petitioner_street_number',
                    type: 'text',
                    props: { placeholder: 'Street number and name' },
                    label: createLocalizedText('Street Number and Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'petitioner_apt_ste_flr',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Apt.', value: 'apt' },
                        { label: 'Ste.', value: 'ste' },
                        { label: 'Flr.', value: 'flr' }
                      ]
                    },
                    label: createLocalizedText('Apartment/Suite/Floor (if applicable)'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'petitioner_apt_number',
                    type: 'text',
                    props: { placeholder: 'Number' },
                    label: createLocalizedText('Apt./Ste./Flr. Number'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'petitioner_city',
                    type: 'text',
                    props: { placeholder: 'City' },
                    label: createLocalizedText('City or Town'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'petitioner_state',
                    type: 'select',
                    props: {
                      options: [
                        { label: 'Select State', value: '' },
                        { label: 'Alabama', value: 'AL' },
                        { label: 'Alaska', value: 'AK' },
                        { label: 'Arizona', value: 'AZ' },
                        { label: 'Arkansas', value: 'AR' },
                        { label: 'California', value: 'CA' },
                        { label: 'Colorado', value: 'CO' },
                        { label: 'Connecticut', value: 'CT' },
                        { label: 'Delaware', value: 'DE' },
                        { label: 'Florida', value: 'FL' },
                        { label: 'Georgia', value: 'GA' },
                        { label: 'Hawaii', value: 'HI' },
                        { label: 'Idaho', value: 'ID' },
                        { label: 'Illinois', value: 'IL' },
                        { label: 'Indiana', value: 'IN' },
                        { label: 'Iowa', value: 'IA' },
                        { label: 'Kansas', value: 'KS' },
                        { label: 'Kentucky', value: 'KY' },
                        { label: 'Louisiana', value: 'LA' },
                        { label: 'Maine', value: 'ME' },
                        { label: 'Maryland', value: 'MD' },
                        { label: 'Massachusetts', value: 'MA' },
                        { label: 'Michigan', value: 'MI' },
                        { label: 'Minnesota', value: 'MN' },
                        { label: 'Mississippi', value: 'MS' },
                        { label: 'Missouri', value: 'MO' },
                        { label: 'Montana', value: 'MT' },
                        { label: 'Nebraska', value: 'NE' },
                        { label: 'Nevada', value: 'NV' },
                        { label: 'New Hampshire', value: 'NH' },
                        { label: 'New Jersey', value: 'NJ' },
                        { label: 'New Mexico', value: 'NM' },
                        { label: 'New York', value: 'NY' },
                        { label: 'North Carolina', value: 'NC' },
                        { label: 'North Dakota', value: 'ND' },
                        { label: 'Ohio', value: 'OH' },
                        { label: 'Oklahoma', value: 'OK' },
                        { label: 'Oregon', value: 'OR' },
                        { label: 'Pennsylvania', value: 'PA' },
                        { label: 'Rhode Island', value: 'RI' },
                        { label: 'South Carolina', value: 'SC' },
                        { label: 'South Dakota', value: 'SD' },
                        { label: 'Tennessee', value: 'TN' },
                        { label: 'Texas', value: 'TX' },
                        { label: 'Utah', value: 'UT' },
                        { label: 'Vermont', value: 'VT' },
                        { label: 'Virginia', value: 'VA' },
                        { label: 'Washington', value: 'WA' },
                        { label: 'West Virginia', value: 'WV' },
                        { label: 'Wisconsin', value: 'WI' },
                        { label: 'Wyoming', value: 'WY' }
                      ]
                    },
                    label: createLocalizedText('State'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'petitioner_zip',
                    type: 'text',
                    props: { placeholder: 'ZIP Code' },
                    label: createLocalizedText('ZIP Code'),
                    required: true,
                    validation: []
                  }
                ]
              },
              {
                id: 'group_petitioner_contact_details',
                title: createLocalizedText('Contact Details'),
                items: [
                  {
                    id: 'petitioner_daytime_phone',
                    type: 'tel',
                    props: { placeholder: '(xxx) xxx-xxxx' },
                    label: createLocalizedText('Daytime Telephone Number'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'petitioner_mobile_phone',
                    type: 'tel',
                    props: { placeholder: '(xxx) xxx-xxxx' },
                    label: createLocalizedText('Mobile Telephone Number'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'petitioner_email',
                    type: 'email',
                    props: { placeholder: 'your.email@example.com' },
                    label: createLocalizedText('Email Address'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_beneficiary_info',
        title: createLocalizedText('Part 2: Information About Your Relative (Beneficiary)'),
        sections: [
          {
            id: 'section_beneficiary_basic',
            title: createLocalizedText('Basic Information About Beneficiary'),
            groups: [
              {
                id: 'group_beneficiary_name',
                title: createLocalizedText('Full Name'),
                items: [
                  {
                    id: 'beneficiary_family_name',
                    type: 'text',
                    props: { placeholder: 'Last name as shown on identity documents' },
                    label: createLocalizedText('Family Name (Last Name)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_given_name',
                    type: 'text',
                    props: { placeholder: 'First name as shown on identity documents' },
                    label: createLocalizedText('Given Name (First Name)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_middle_name',
                    type: 'text',
                    props: { placeholder: 'Middle name if applicable' },
                    label: createLocalizedText('Middle Name (if applicable)'),
                    required: false,
                    validation: []
                  }
                ]
              },
              {
                id: 'group_beneficiary_other_names',
                title: createLocalizedText('Other Names Used'),
                items: [
                  {
                    id: 'beneficiary_other_names_used',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Has your relative ever used other names?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_other_names_list',
                    type: 'textarea',
                    props: { placeholder: 'List all other names used, separated by commas', rows: 3 },
                    label: createLocalizedText('If yes, list all other names used:'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_beneficiary_personal',
            title: createLocalizedText('Personal Details'),
            groups: [
              {
                id: 'group_beneficiary_birth',
                title: createLocalizedText('Birth Information'),
                items: [
                  {
                    id: 'beneficiary_dob',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Birth (mm/dd/yyyy)'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_gender',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Male', value: 'male' },
                        { label: 'Female', value: 'female' }
                      ]
                    },
                    label: createLocalizedText('Gender'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_country_birth',
                    type: 'text',
                    props: { placeholder: 'Country where your relative was born' },
                    label: createLocalizedText('Country of Birth'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_city_birth',
                    type: 'text',
                    props: { placeholder: 'City where your relative was born' },
                    label: createLocalizedText('City of Birth'),
                    required: true,
                    validation: []
                  }
                ]
              },
              {
                id: 'group_beneficiary_citizenship',
                title: createLocalizedText('Citizenship and Status'),
                items: [
                  {
                    id: 'beneficiary_country_citizenship',
                    type: 'text',
                    props: { placeholder: 'Country of citizenship' },
                    label: createLocalizedText('Country of Citizenship or Nationality'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_alien_number',
                    type: 'text',
                    props: { placeholder: 'A-Number (if applicable)' },
                    label: createLocalizedText('USCIS Online Account Number or A-Number (if any)'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'beneficiary_in_us',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Is your relative currently in the United States?'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_beneficiary_address',
            title: createLocalizedText('Address Information'),
            groups: [
              {
                id: 'group_beneficiary_current_address',
                title: createLocalizedText('Current Physical Address'),
                items: [
                  {
                    id: 'beneficiary_street_number',
                    type: 'text',
                    props: { placeholder: 'Street number and name' },
                    label: createLocalizedText('Street Number and Name'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_apt_ste_flr',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Apt.', value: 'apt' },
                        { label: 'Ste.', value: 'ste' },
                        { label: 'Flr.', value: 'flr' }
                      ]
                    },
                    label: createLocalizedText('Apartment/Suite/Floor (if applicable)'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'beneficiary_apt_number',
                    type: 'text',
                    props: { placeholder: 'Number' },
                    label: createLocalizedText('Apt./Ste./Flr. Number'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'beneficiary_city',
                    type: 'text',
                    props: { placeholder: 'City' },
                    label: createLocalizedText('City or Town'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_state_province',
                    type: 'text',
                    props: { placeholder: 'State/Province' },
                    label: createLocalizedText('State or Province'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'beneficiary_postal_code',
                    type: 'text',
                    props: { placeholder: 'Postal/ZIP Code' },
                    label: createLocalizedText('Postal Code'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'beneficiary_country',
                    type: 'text',
                    props: { placeholder: 'Country' },
                    label: createLocalizedText('Country'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_relationship_info',
        title: createLocalizedText('Part 3: Relationship Information'),
        sections: [
          {
            id: 'section_relationship',
            title: createLocalizedText('Relationship to Beneficiary'),
            groups: [
              {
                id: 'group_relationship_type',
                title: createLocalizedText('Type of Relationship'),
                items: [
                  {
                    id: 'relationship_type',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Spouse', value: 'spouse' },
                        { label: 'Unmarried child under 21 years old', value: 'child_under21' },
                        { label: 'Unmarried child 21 years old or older', value: 'child_over21' },
                        { label: 'Married child', value: 'married_child' },
                        { label: 'Parent', value: 'parent' },
                        { label: 'Brother or sister', value: 'sibling' }
                      ]
                    },
                    label: createLocalizedText('What is your relationship to the beneficiary?'),
                    required: true,
                    validation: []
                  }
                ]
              },
              {
                id: 'group_marriage_info',
                title: createLocalizedText('Marriage Information (if applicable)'),
                items: [
                  {
                    id: 'petitioner_married',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Are you currently married?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'marriage_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Current Marriage (if married)'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'marriage_place',
                    type: 'text',
                    props: { placeholder: 'City, State/Province, Country' },
                    label: createLocalizedText('Place of Current Marriage'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'previous_marriages_petitioner',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Have you been previously married?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'beneficiary_married',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Is the beneficiary currently married?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'previous_marriages_beneficiary',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Has the beneficiary been previously married?'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_additional_info',
        title: createLocalizedText('Part 4: Additional Information'),
        sections: [
          {
            id: 'section_immigration_info',
            title: createLocalizedText('Immigration Information'),
            groups: [
              {
                id: 'group_previous_petitions',
                title: createLocalizedText('Previous Petitions'),
                items: [
                  {
                    id: 'previous_i130_filed',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Have you ever filed a petition for this beneficiary or any other alien before?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'previous_petition_details',
                    type: 'textarea',
                    props: { placeholder: 'If yes, provide details including when, where, result, and receipt/case numbers', rows: 4 },
                    label: createLocalizedText('If yes, provide details:'),
                    required: false,
                    validation: []
                  }
                ]
              },
              {
                id: 'group_beneficiary_immigration',
                title: createLocalizedText('Beneficiary Immigration History'),
                items: [
                  {
                    id: 'beneficiary_us_entry_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date beneficiary last arrived in U.S. (if applicable)'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'beneficiary_i94_number',
                    type: 'text',
                    props: { placeholder: 'I-94 Arrival/Departure Record Number' },
                    label: createLocalizedText('I-94 Arrival/Departure Record Number (if applicable)'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'beneficiary_passport_number',
                    type: 'text',
                    props: { placeholder: 'Passport number' },
                    label: createLocalizedText('Passport Number'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'beneficiary_passport_country',
                    type: 'text',
                    props: { placeholder: 'Country that issued passport' },
                    label: createLocalizedText('Country of Issuance for Passport'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'beneficiary_passport_expiry',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date Passport Expires'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_signature_info',
        title: createLocalizedText('Part 5: Petitioner\'s Statement and Signature'),
        sections: [
          {
            id: 'section_petitioner_statement',
            title: createLocalizedText('Petitioner\'s Statement'),
            groups: [
              {
                id: 'group_statement_options',
                title: createLocalizedText('Statement Type'),
                items: [
                  {
                    id: 'statement_type',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'I can read and understand English, and I have read and understand every question and instruction on this petition and my answer to every question.', value: 'english' },
                        { label: 'The interpreter named in Part 6 read to me every question and instruction on this petition and my answer to every question in a language in which I am fluent. I understood all of this information as interpreted.', value: 'interpreter' },
                        { label: 'At my request, the preparer named in Part 7 prepared this petition for me based only upon information I provided or authorized.', value: 'preparer' }
                      ]
                    },
                    label: createLocalizedText('Select the appropriate statement:'),
                    required: true,
                    validation: []
                  }
                ]
              },
              {
                id: 'group_signature',
                title: createLocalizedText('Signature Information'),
                items: [
                  {
                    id: 'signature_date',
                    type: 'date',
                    props: {},
                    label: createLocalizedText('Date of Signature'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'signature_confirmation',
                    type: 'checkbox',
                    props: {
                      options: [
                        { label: 'I certify, under penalty of perjury, that I provided or authorized all of the information in my petition, I understand all of the information contained in, and submitted with, my petition, and that all of this information is complete, true, and correct.', value: 'confirmed' }
                      ]
                    },
                    label: createLocalizedText('Signature Confirmation'),
                    required: true,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      },
      {
        id: 'page_interpreter_preparer',
        title: createLocalizedText('Part 6 & 7: Interpreter and Preparer Information'),
        sections: [
          {
            id: 'section_interpreter',
            title: createLocalizedText('Interpreter Information (if applicable)'),
            groups: [
              {
                id: 'group_interpreter_used',
                title: createLocalizedText('Interpreter Usage'),
                items: [
                  {
                    id: 'interpreter_used',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Did you use an interpreter to complete this petition?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'interpreter_name',
                    type: 'text',
                    props: { placeholder: 'Interpreter full name' },
                    label: createLocalizedText('Interpreter\'s Full Name'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'interpreter_language',
                    type: 'text',
                    props: { placeholder: 'Language used' },
                    label: createLocalizedText('Name of Language Used'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          },
          {
            id: 'section_preparer',
            title: createLocalizedText('Preparer Information (if applicable)'),
            groups: [
              {
                id: 'group_preparer_used',
                title: createLocalizedText('Preparer Usage'),
                items: [
                  {
                    id: 'preparer_used',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Did someone help you prepare this petition?'),
                    required: true,
                    validation: []
                  },
                  {
                    id: 'preparer_name',
                    type: 'text',
                    props: { placeholder: 'Preparer full name' },
                    label: createLocalizedText('Preparer\'s Full Name'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'preparer_business_name',
                    type: 'text',
                    props: { placeholder: 'Business or organization name' },
                    label: createLocalizedText('Preparer\'s Business or Organization Name'),
                    required: false,
                    validation: []
                  },
                  {
                    id: 'preparer_is_attorney',
                    type: 'radio',
                    props: {
                      options: [
                        { label: 'Yes', value: 'yes' },
                        { label: 'No', value: 'no' }
                      ]
                    },
                    label: createLocalizedText('Is the preparer an attorney or accredited representative?'),
                    required: false,
                    validation: []
                  }
                ]
              }
            ]
          }
        ]
      }
    ],
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  }
]

export const useQuestionnaireStore = defineStore('questionnaire', () => {
  // State — empty by default; fetchQuestionnaires() populates from the
  // backend. Legacy seed questionnaires (immigration, divorce, etc.) lived
  // only as SysCase demo bootstrap; they have no place under per-tenant
  // persistence and are dropped.
  const questionnaires = ref<Questionnaire[]>([])
  const currentQuestionnaire = ref<Questionnaire | null>(null)
  const selectedItemId = ref<string | null>(null)
  const undoStack = ref<Questionnaire[]>([])
  const redoStack = ref<Questionnaire[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)
  const userGroupTemplates = ref<GroupTemplate[]>([])  // User-created templates

  // Getters
  const selectedItem = computed(() => {
    if (!currentQuestionnaire.value || !selectedItemId.value) return null

    for (const page of currentQuestionnaire.value.pages) {
      for (const section of page.sections) {
        for (const group of section.groups) {
          const item = group.items.find(item => item.id === selectedItemId.value)
          if (item) return item
        }
      }
    }
    return null
  })

  const canUndo = computed(() => undoStack.value.length > 0)
  const canRedo = computed(() => redoStack.value.length > 0)

  // All available group templates (defaults + user created)
  const allGroupTemplates = computed(() => {
    const customComponentsStore = useCustomComponentsStore()
    return [...defaultGroupTemplates, ...customComponentsStore.allCustomComponents]
  })

  // Actions
  const createNew = (name?: string) => {
    const questionnaire: Questionnaire = {
      version: '1.0.0',
      id: generateId('questionnaire'),
      name: createLocalizedText(name || 'New Questionnaire'),
      description: createLocalizedText('A custom intake form for bookings'),
      pages: [
        {
          id: generateId('page'),
          title: createLocalizedText('Basic Information'),
          sections: [
            {
              id: generateId('section'),
              title: createLocalizedText('Personal Details'),
              groups: [
                {
                  id: generateId('group'),
                  title: createLocalizedText('Default Group'),
                  items: []
                }
              ]
            }
          ]
        }
      ],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }

    questionnaires.value.push(questionnaire)
    currentQuestionnaire.value = questionnaire
    saveToUndoStack()
  }

  const loadQuestionnaire = (id: string) => {
    const questionnaire = questionnaires.value.find(q => q.id === id)
    if (questionnaire) {
      currentQuestionnaire.value = questionnaire
      clearStacks()
    }
  }

  const updateQuestionnaire = (updates: Partial<Questionnaire>) => {
    if (!currentQuestionnaire.value) return

    saveToUndoStack()
    currentQuestionnaire.value = {
      ...currentQuestionnaire.value,
      ...updates,
      updatedAt: new Date().toISOString()
    }
  }

  const addComponent = (
    componentType: ComponentType,
    position?: { pageIndex: number; sectionIndex: number; groupIndex?: number; itemIndex?: number }
  ): QuestionnaireItem => {
    if (!currentQuestionnaire.value) {
      throw new Error('No active questionnaire')
    }

    // Check if it's a custom component first
    const customComponentsStore = useCustomComponentsStore()
    const customComponent = customComponentsStore.allCustomComponents.find(c => c.id === componentType)

    let component
    if (customComponent) {
      // It's a custom component
      component = {
        title: customComponent.name,
        description: customComponent.description || { fallback: '' },
        defaultProps: {
          label: customComponent.defaultLabel,
          placeholder: customComponent.defaultPlaceholder,
          required: customComponent.required,
          ...customComponent.customProps
        }
      }
    } else {
      // It's a standard component
      component = componentRegistry[componentType]
      if (!component) {
        throw new Error(`Unknown component type: ${componentType}`)
      }
    }

    const newItem: QuestionnaireItem = {
      id: generateId('item'),
      type: componentType,
      props: { ...component.defaultProps },
      label: createLocalizedText(component.title.fallback),
      description: component.description.fallback ? createLocalizedText(component.description.fallback) : undefined,
      validation: [],
      required: component.defaultProps?.required || false
    }

    saveToUndoStack()

    // Add to specified position or first available group
    if (position) {
      const page = currentQuestionnaire.value.pages[position.pageIndex]
      const section = page?.sections[position.sectionIndex]
      const groupIndex = position.groupIndex ?? 0
      const group = section?.groups[groupIndex]
      if (group) {
        if (position.itemIndex !== undefined) {
          group.items.splice(position.itemIndex, 0, newItem)
        } else {
          group.items.push(newItem)
        }
      }
    } else {
      // Add to first group of first section of first page
      const firstPage = currentQuestionnaire.value.pages[0]
      if (firstPage && firstPage.sections.length > 0 && firstPage.sections[0].groups.length > 0) {
        firstPage.sections[0].groups[0].items.push(newItem)
      }
    }

    return newItem
  }

  const updateItem = (itemId: string, updates: Partial<QuestionnaireItem>) => {
    if (!currentQuestionnaire.value) return

    saveToUndoStack()

    for (const page of currentQuestionnaire.value.pages) {
      for (const section of page.sections) {
        for (const group of section.groups) {
          const itemIndex = group.items.findIndex(item => item.id === itemId)
          if (itemIndex !== -1) {
            group.items[itemIndex] = {
              ...group.items[itemIndex],
              ...updates
            }
            return
          }
        }
      }
    }
  }

  const duplicateItem = (itemId: string): QuestionnaireItem | null => {
    if (!currentQuestionnaire.value) return null

    saveToUndoStack()

    for (const page of currentQuestionnaire.value.pages) {
      for (const section of page.sections) {
        for (const group of section.groups) {
          const itemIndex = group.items.findIndex(item => item.id === itemId)
          if (itemIndex !== -1) {
            const originalItem = group.items[itemIndex]
            const duplicatedItem: QuestionnaireItem = {
              ...originalItem,
              id: generateId('item'),
              label: originalItem.label ? {
                ...originalItem.label,
                fallback: `${originalItem.label.fallback} (Copy)`
              } : undefined
            }
            group.items.splice(itemIndex + 1, 0, duplicatedItem)
            return duplicatedItem
          }
        }
      }
    }
    return null
  }

  const deleteItem = (itemId: string) => {
    if (!currentQuestionnaire.value) return

    saveToUndoStack()

    for (const page of currentQuestionnaire.value.pages) {
      for (const section of page.sections) {
        for (const group of section.groups) {
          const itemIndex = group.items.findIndex(item => item.id === itemId)
          if (itemIndex !== -1) {
            group.items.splice(itemIndex, 1)
            if (selectedItemId.value === itemId) {
              selectedItemId.value = null
            }
            return
          }
        }
      }
    }
  }

  const moveItem = (
    itemId: string,
    targetPageIndex: number,
    targetSectionIndex: number,
    targetItemIndex: number
  ) => {
    if (!currentQuestionnaire.value) return

    // Find and remove the item
    let item: QuestionnaireItem | null = null
    for (const page of currentQuestionnaire.value.pages) {
      for (const section of page.sections) {
        for (const group of section.groups) {
          const itemIndex = group.items.findIndex(i => i.id === itemId)
          if (itemIndex !== -1) {
            item = group.items.splice(itemIndex, 1)[0]
            break
          }
        }
        if (item) break
      }
      if (item) break
    }

    if (!item) return

    saveToUndoStack()

    // Insert at target position (for now, add to first group)
    const targetPage = currentQuestionnaire.value.pages[targetPageIndex]
    const targetSection = targetPage?.sections[targetSectionIndex]
    if (targetSection && targetSection.groups.length > 0) {
      targetSection.groups[0].items.splice(targetItemIndex, 0, item)
    }
  }

  const addPage = (): QuestionnairePage => {
    if (!currentQuestionnaire.value) {
      throw new Error('No active questionnaire')
    }

    saveToUndoStack()

    const newPage: QuestionnairePage = {
      id: generateId('page'),
      title: createLocalizedText(`Page ${currentQuestionnaire.value.pages.length + 1}`),
      sections: [
        {
          id: generateId('section'),
          title: createLocalizedText('Section'),
          groups: [
            {
              id: generateId('group'),
              title: createLocalizedText('Default Group'),
              items: []
            }
          ]
        }
      ]
    }

    currentQuestionnaire.value.pages.push(newPage)
    return newPage
  }

  const addSection = (pageIndex: number): QuestionnaireSection => {
    if (!currentQuestionnaire.value) {
      throw new Error('No active questionnaire')
    }

    const page = currentQuestionnaire.value.pages[pageIndex]
    if (!page) {
      throw new Error('Page not found')
    }

    saveToUndoStack()

    const newSection: QuestionnaireSection = {
      id: generateId('section'),
      title: createLocalizedText(`Section ${page.sections.length + 1}`),
      groups: [
        {
          id: generateId('group'),
          title: createLocalizedText('Default Group'),
          items: []
        }
      ]
    }

    page.sections.push(newSection)
    return newSection
  }

  const importQuestionnaire = (questionnaire: Questionnaire) => {
    // Validate and migrate if necessary
    const validatedQuestionnaire = validateAndMigrate(questionnaire)
    questionnaires.value.push(validatedQuestionnaire)
    currentQuestionnaire.value = validatedQuestionnaire
    clearStacks()
  }

  const exportQuestionnaire = (): Questionnaire | null => {
    return currentQuestionnaire.value
  }

  const undo = () => {
    if (!canUndo.value || !currentQuestionnaire.value) return

    redoStack.value.push(JSON.parse(JSON.stringify(currentQuestionnaire.value)))
    const previous = undoStack.value.pop()
    if (previous) {
      currentQuestionnaire.value = previous
    }
  }

  const redo = () => {
    if (!canRedo.value) return

    if (currentQuestionnaire.value) {
      undoStack.value.push(JSON.parse(JSON.stringify(currentQuestionnaire.value)))
    }
    const next = redoStack.value.pop()
    if (next) {
      currentQuestionnaire.value = next
    }
  }

  // Helper functions
  const saveToUndoStack = () => {
    if (currentQuestionnaire.value) {
      undoStack.value.push(JSON.parse(JSON.stringify(currentQuestionnaire.value)))
      if (undoStack.value.length > 50) {
        undoStack.value.shift() // Keep stack size manageable
      }
      redoStack.value = [] // Clear redo stack on new action
    }
  }

  const clearStacks = () => {
    undoStack.value = []
    redoStack.value = []
  }

  const validateAndMigrate = (questionnaire: any): Questionnaire => {
    // Basic validation and migration logic
    if (!questionnaire.version) {
      questionnaire.version = '1.0.0'
    }
    if (!questionnaire.id) {
      questionnaire.id = generateId('questionnaire')
    }
    if (!questionnaire.pages) {
      questionnaire.pages = []
    }

    // Ensure all items have IDs and migrate old structure
    for (const page of questionnaire.pages) {
      if (!page.id) page.id = generateId('page')
      for (const section of page.sections || []) {
        if (!section.id) section.id = generateId('section')

        // Migrate old structure: if section has items directly, move them to groups
        if ((section as any).items && !section.groups) {
          section.groups = [
            {
              id: generateId('group'),
              title: createLocalizedText('Default Group'),
              items: (section as any).items
            }
          ]
          delete (section as any).items
        }

        // Ensure groups exist
        if (!section.groups) {
          section.groups = [
            {
              id: generateId('group'),
              title: createLocalizedText('Default Group'),
              items: []
            }
          ]
        }

        for (const group of section.groups) {
          if (!group.id) group.id = generateId('group')
          for (const item of group.items || []) {
            if (!item.id) item.id = generateId('item')
          }
        }
      }
    }

    return questionnaire as Questionnaire
  }

  const selectItem = (itemId: string | null) => {
    selectedItemId.value = itemId
  }

  const addGroup = (pageIndex: number, sectionIndex: number): QuestionnaireGroup => {
    if (!currentQuestionnaire.value) {
      throw new Error('No active questionnaire')
    }

    const page = currentQuestionnaire.value.pages[pageIndex]
    const section = page?.sections[sectionIndex]
    if (!section) {
      throw new Error('Section not found')
    }

    saveToUndoStack()

    const newGroup: QuestionnaireGroup = {
      id: generateId('group'),
      title: createLocalizedText(`Group ${section.groups.length + 1}`),
      items: []
    }

    section.groups.push(newGroup)
    return newGroup
  }

  const updateGroup = (groupId: string, updates: Partial<QuestionnaireGroup>) => {
    if (!currentQuestionnaire.value) return

    saveToUndoStack()

    for (const page of currentQuestionnaire.value.pages) {
      for (const section of page.sections) {
        const groupIndex = section.groups.findIndex(group => group.id === groupId)
        if (groupIndex !== -1) {
          section.groups[groupIndex] = {
            ...section.groups[groupIndex],
            ...updates
          }
          return
        }
      }
    }
  }

  const duplicateGroup = (groupId: string): QuestionnaireGroup | null => {
    if (!currentQuestionnaire.value) return null

    saveToUndoStack()

    for (const page of currentQuestionnaire.value.pages) {
      for (const section of page.sections) {
        const groupIndex = section.groups.findIndex(group => group.id === groupId)
        if (groupIndex !== -1) {
          const originalGroup = section.groups[groupIndex]
          const duplicatedGroup: QuestionnaireGroup = {
            ...originalGroup,
            id: generateId('group'),
            title: originalGroup.title ? {
              ...originalGroup.title,
              fallback: `${originalGroup.title.fallback} (Copy)`
            } : createLocalizedText('Group (Copy)'),
            items: originalGroup.items.map(item => ({
              ...item,
              id: generateId('item')
            }))
          }
          section.groups.splice(groupIndex + 1, 0, duplicatedGroup)
          return duplicatedGroup
        }
      }
    }
    return null
  }

  const deleteGroup = (groupId: string) => {
    if (!currentQuestionnaire.value) return

    saveToUndoStack()

    for (const page of currentQuestionnaire.value.pages) {
      for (const section of page.sections) {
        const groupIndex = section.groups.findIndex(group => group.id === groupId)
        if (groupIndex !== -1 && section.groups.length > 1) {
          section.groups.splice(groupIndex, 1)
          return
        }
      }
    }
  }

  const saveGroupAsTemplate = (group: QuestionnaireGroup, templateName?: string, description?: string) => {
    const template: GroupTemplate = {
      id: `user_template_${Date.now()}`,
      name: createLocalizedText(templateName || group.title?.fallback || 'Custom Template'),
      description: description ? createLocalizedText(description) : undefined,
      icon: 'mdi-folder-star',
      previewText: `${group.items.length} fields`,
      group: {
        ...group,
        id: generateId('group'), // Give the template group a new ID
        items: group.items.map(item => ({
          ...item,
          id: item.id.split('_').pop() || item.id // Remove instance prefixes for template
        }))
      }
    }

    userGroupTemplates.value.push(template)

    // Save to localStorage for persistence
    localStorage.setItem('userGroupTemplates', JSON.stringify(userGroupTemplates.value))

    return template
  }

  const updateGroupTemplate = (template: GroupTemplate) => {
    const index = userGroupTemplates.value.findIndex(t => t.id === template.id)
    if (index !== -1) {
      userGroupTemplates.value[index] = template
      localStorage.setItem('userGroupTemplates', JSON.stringify(userGroupTemplates.value))
    }
  }

  const deleteGroupTemplate = (templateId: string) => {
    const index = userGroupTemplates.value.findIndex(t => t.id === templateId)
    if (index !== -1) {
      userGroupTemplates.value.splice(index, 1)
      localStorage.setItem('userGroupTemplates', JSON.stringify(userGroupTemplates.value))
    }
  }

  const loadUserTemplates = () => {
    try {
      const saved = localStorage.getItem('userGroupTemplates')
      if (saved) {
        userGroupTemplates.value = JSON.parse(saved)
      }
    } catch (error) {
      console.warn('Failed to load user group templates:', error)
    }
  }

  const clearError = () => {
    error.value = null
  }

  // ---- Phase 15e backend-backed CRUD ----

  const fetchQuestionnaires = async () => {
    isLoading.value = true
    error.value = null
    try {
      const listResp = await intakeApi.listQuestionnaireTemplates(false)
      const items = listResp.data.data?.items ?? []
      const full = await Promise.all(
        items.map(async item => {
          const fullResp = await intakeApi.getQuestionnaireTemplate(item.questionnaireTemplateId)
          return toQuestionnaire(fullResp.data.data!)
        })
      )
      full.sort((a, b) => (b.createdAt || '').localeCompare(a.createdAt || ''))
      questionnaires.value = full
    } catch (err) {
      error.value = 'Failed to fetch questionnaires'
      console.error('Error fetching questionnaires:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const saveCurrentQuestionnaire = async (): Promise<Questionnaire | null> => {
    if (!currentQuestionnaire.value) return null
    isLoading.value = true
    error.value = null
    try {
      const q = currentQuestionnaire.value
      const name = q.name?.fallback || 'Untitled questionnaire'
      const definitionJson = JSON.stringify(q)
      let serverId: string
      if (isServerId(q.id)) {
        const resp = await intakeApi.updateQuestionnaireTemplate(q.id, {
          name,
          version: q.version,
          definitionJson
        })
        serverId = resp.data.data!.questionnaireTemplateId
      } else {
        const resp = await intakeApi.createQuestionnaireTemplate({
          name,
          version: q.version,
          definitionJson
        })
        serverId = resp.data.data!.questionnaireTemplateId
      }
      const fresh = toQuestionnaire(
        (await intakeApi.getQuestionnaireTemplate(serverId)).data.data!
      )
      const idx = questionnaires.value.findIndex(x => x.id === q.id || x.id === serverId)
      if (idx >= 0) {
        questionnaires.value[idx] = fresh
      } else {
        questionnaires.value.unshift(fresh)
      }
      currentQuestionnaire.value = fresh
      return fresh
    } catch (err) {
      error.value = 'Failed to save questionnaire'
      console.error('Error saving questionnaire:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  const deleteQuestionnaire = async (questionnaireId: string) => {
    isLoading.value = true
    error.value = null
    try {
      if (isServerId(questionnaireId)) {
        await intakeApi.deleteQuestionnaireTemplate(questionnaireId)
      }
      const idx = questionnaires.value.findIndex(q => q.id === questionnaireId)
      if (idx >= 0) questionnaires.value.splice(idx, 1)
      if (currentQuestionnaire.value?.id === questionnaireId) {
        currentQuestionnaire.value = null
      }
    } catch (err) {
      error.value = 'Failed to delete questionnaire'
      console.error('Error deleting questionnaire:', err)
      throw err
    } finally {
      isLoading.value = false
    }
  }

  return {
    // State
    questionnaires,
    currentQuestionnaire,
    selectedItemId,
    isLoading,
    error,
    userGroupTemplates,

    // Getters
    selectedItem,
    canUndo,
    canRedo,
    allGroupTemplates,

    // Actions
    createNew,
    loadQuestionnaire,
    updateQuestionnaire,
    addComponent,
    updateItem,
    duplicateItem,
    deleteItem,
    moveItem,
    addPage,
    addSection,
    addGroup,
    updateGroup,
    duplicateGroup,
    deleteGroup,
    saveGroupAsTemplate,
    updateGroupTemplate,
    deleteGroupTemplate,
    loadUserTemplates,
    importQuestionnaire,
    exportQuestionnaire,
    undo,
    redo,
    selectItem,
    clearError,
    fetchQuestionnaires,
    saveCurrentQuestionnaire,
    deleteQuestionnaire
  }
})