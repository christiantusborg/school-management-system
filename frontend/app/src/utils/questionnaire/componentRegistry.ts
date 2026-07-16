// @ts-nocheck — ported SysCase file; TS strict cleanup is a follow-up
import type { ComponentRegistry, ComponentSchema, LocalizableText, GroupTemplate, QuestionnaireItem } from '@quvian/shared/types/questionnaire'

// Helper to create localized text
const t = (text: string): LocalizableText => ({ fallback: text })

export const componentRegistry: ComponentRegistry = {
  // Input Components
  text: {
    type: 'text',
    title: t('Text Input'),
    description: t('Single line text field'),
    icon: 'mdi-form-textbox',
    category: 'input',
    properties: {
      placeholder: { type: 'string', title: 'Placeholder' },
      maxLength: { type: 'number', title: 'Max Length', minimum: 1 },
      mask: { type: 'string', title: 'Input Mask' }
    },
    defaultProps: {
      placeholder: 'Enter text...'
    },
    validation: [],
    accessibility: {
      role: 'textbox'
    }
  },

  textarea: {
    type: 'textarea',
    title: t('Text Area'),
    description: t('Multi-line text field'),
    icon: 'mdi-form-textarea',
    category: 'input',
    properties: {
      placeholder: { type: 'string', title: 'Placeholder' },
      rows: { type: 'number', title: 'Rows', minimum: 2, default: 4 },
      maxLength: { type: 'number', title: 'Max Length', minimum: 1 }
    },
    defaultProps: {
      placeholder: 'Enter your message...',
      rows: 4
    },
    validation: [],
    accessibility: {
      role: 'textbox'
    }
  },

  richtext: {
    type: 'richtext',
    title: t('Dynamic Text Block'),
    description: t('Text content with variable replacement from form fields'),
    icon: 'mdi-text-box-search',
    category: 'special', // Updated category
    properties: {
      showInLive: { type: 'boolean', title: 'Show in Live Questionnaire', default: true },
      minHeight: { type: 'number', title: 'Minimum Height (px)', default: 200 }
    },
    defaultProps: {
      showInLive: true,
      minHeight: 200,
      content: ''
    },
    validation: [],
    accessibility: {
      role: 'textbox'
    }
  },

  number: {
    type: 'number',
    title: t('Number Input'),
    description: t('Numeric input field'),
    icon: 'mdi-numeric',
    category: 'input',
    properties: {
      min: { type: 'number', title: 'Minimum Value' },
      max: { type: 'number', title: 'Maximum Value' },
      step: { type: 'number', title: 'Step', default: 1 },
      placeholder: { type: 'string', title: 'Placeholder' }
    },
    defaultProps: {
      placeholder: 'Enter number...',
      step: 1
    },
    validation: [],
    accessibility: {
      role: 'spinbutton'
    }
  },

  email: {
    type: 'email',
    title: t('Email Input'),
    description: t('Email address field'),
    icon: 'mdi-email',
    category: 'input',
    properties: {
      placeholder: { type: 'string', title: 'Placeholder' }
    },
    defaultProps: {
      placeholder: 'Enter email address...'
    },
    validation: [
      { type: 'email', message: t('Please enter a valid email address') }
    ],
    accessibility: {
      role: 'textbox'
    }
  },

  phone: {
    type: 'phone',
    title: t('Phone Input'),
    description: t('Phone number field'),
    icon: 'mdi-phone',
    category: 'input',
    properties: {
      placeholder: { type: 'string', title: 'Placeholder' },
      countryCode: { type: 'string', title: 'Default Country Code', default: 'US' }
    },
    defaultProps: {
      placeholder: 'Enter phone number...',
      countryCode: 'US'
    },
    validation: [],
    accessibility: {
      role: 'textbox'
    }
  },

  date: {
    type: 'date',
    title: t('Date Picker'),
    description: t('Date selection field'),
    icon: 'mdi-calendar',
    category: 'input',
    properties: {
      minDate: { type: 'string', title: 'Minimum Date' },
      maxDate: { type: 'string', title: 'Maximum Date' },
      format: { type: 'string', title: 'Date Format', default: 'YYYY-MM-DD' }
    },
    defaultProps: {
      format: 'YYYY-MM-DD'
    },
    validation: [],
    accessibility: {
      role: 'textbox'
    }
  },

  time: {
    type: 'time',
    title: t('Time Picker'),
    description: t('Time selection field'),
    icon: 'mdi-clock',
    category: 'input',
    properties: {
      format: { type: 'string', title: 'Time Format', default: 'HH:mm' },
      step: { type: 'number', title: 'Step (minutes)', default: 15 }
    },
    defaultProps: {
      format: 'HH:mm',
      step: 15
    },
    validation: [],
    accessibility: {
      role: 'textbox'
    }
  },

  datetime: {
    type: 'datetime',
    title: t('Date & Time Picker'),
    description: t('Date and time selection field'),
    icon: 'mdi-calendar-clock',
    category: 'input',
    properties: {
      minDate: { type: 'string', title: 'Minimum Date' },
      maxDate: { type: 'string', title: 'Maximum Date' },
      format: { type: 'string', title: 'DateTime Format', default: 'YYYY-MM-DD HH:mm' }
    },
    defaultProps: {
      format: 'YYYY-MM-DD HH:mm'
    },
    validation: [],
    accessibility: {
      role: 'textbox'
    }
  },

  // Selection Components
  select: {
    type: 'select',
    title: t('Dropdown Select'),
    description: t('Dropdown selection field'),
    icon: 'mdi-form-dropdown',
    category: 'selection',
    properties: {
      options: {
        type: 'array',
        title: 'Options',
        items: {
          type: 'object',
          properties: {
            value: { type: 'string', title: 'Value' },
            label: { type: 'string', title: 'Label' },
            disabled: { type: 'boolean', title: 'Disabled' }
          }
        }
      },
      multiple: { type: 'boolean', title: 'Allow Multiple Selection' },
      searchable: { type: 'boolean', title: 'Searchable' },
      placeholder: { type: 'string', title: 'Placeholder' }
    },
    defaultProps: {
      options: [
        { value: 'option1', label: 'Option 1' },
        { value: 'option2', label: 'Option 2' }
      ],
      placeholder: 'Select an option...'
    },
    validation: [],
    accessibility: {
      role: 'combobox'
    }
  },

  radio: {
    type: 'radio',
    title: t('Radio Buttons'),
    description: t('Single selection from radio buttons'),
    icon: 'mdi-radiobox-marked',
    category: 'selection',
    properties: {
      options: {
        type: 'array',
        title: 'Options',
        items: {
          type: 'object',
          properties: {
            value: { type: 'string', title: 'Value' },
            label: { type: 'string', title: 'Label' },
            disabled: { type: 'boolean', title: 'Disabled' }
          }
        }
      },
      layout: { type: 'string', title: 'Layout', enum: ['vertical', 'horizontal'], default: 'vertical' }
    },
    defaultProps: {
      options: [
        { value: 'option1', label: 'Option 1' },
        { value: 'option2', label: 'Option 2' }
      ],
      layout: 'vertical'
    },
    validation: [],
    accessibility: {
      role: 'radiogroup'
    }
  },

  checkbox: {
    type: 'checkbox',
    title: t('Checkboxes'),
    description: t('Multiple selection with checkboxes'),
    icon: 'mdi-checkbox-marked',
    category: 'selection',
    properties: {
      options: {
        type: 'array',
        title: 'Options',
        items: {
          type: 'object',
          properties: {
            value: { type: 'string', title: 'Value' },
            label: { type: 'string', title: 'Label' },
            disabled: { type: 'boolean', title: 'Disabled' }
          }
        }
      },
      layout: { type: 'string', title: 'Layout', enum: ['vertical', 'horizontal'], default: 'vertical' }
    },
    defaultProps: {
      options: [
        { value: 'option1', label: 'Option 1' },
        { value: 'option2', label: 'Option 2' }
      ],
      layout: 'vertical'
    },
    validation: [],
    accessibility: {
      role: 'group'
    }
  },

  toggle: {
    type: 'toggle',
    title: t('Toggle Switch'),
    description: t('Boolean toggle switch'),
    icon: 'mdi-toggle-switch',
    category: 'selection',
    properties: {
      trueLabel: { type: 'string', title: 'True Label', default: 'Yes' },
      falseLabel: { type: 'string', title: 'False Label', default: 'No' }
    },
    defaultProps: {
      trueLabel: 'Yes',
      falseLabel: 'No'
    },
    validation: [],
    accessibility: {
      role: 'switch'
    }
  },

  // Upload Components
  'file-upload': {
    type: 'file-upload',
    title: t('File Upload'),
    description: t('File upload field'),
    icon: 'mdi-file-upload',
    category: 'upload',
    properties: {
      accept: { type: 'string', title: 'Accepted File Types', default: '*/*' },
      maxSize: { type: 'number', title: 'Max File Size (MB)', default: 10 },
      maxFiles: { type: 'number', title: 'Max Number of Files', default: 1 },
      multiple: { type: 'boolean', title: 'Allow Multiple Files' }
    },
    defaultProps: {
      accept: '*/*',
      maxSize: 10,
      maxFiles: 1,
      multiple: false
    },
    validation: [],
    accessibility: {
      role: 'button'
    }
  },


  // Display Components
  heading: {
    type: 'heading',
    title: t('Heading'),
    description: t('Section heading'),
    icon: 'mdi-format-header-1',
    category: 'display',
    properties: {
      text: { type: 'string', title: 'Heading Text', default: 'Heading' },
      level: { type: 'number', title: 'Heading Level', minimum: 1, maximum: 6, default: 2 }
    },
    defaultProps: {
      text: 'Heading',
      level: 2
    },
    validation: [],
    accessibility: {
      role: 'heading'
    }
  },

  paragraph: {
    type: 'paragraph',
    title: t('Paragraph'),
    description: t('Text paragraph'),
    icon: 'mdi-text',
    category: 'display',
    properties: {
      text: { type: 'string', title: 'Text Content', default: 'Paragraph text...' },
      align: { type: 'string', title: 'Text Alignment', enum: ['left', 'center', 'right'], default: 'left' }
    },
    defaultProps: {
      text: 'Paragraph text...',
      align: 'left'
    },
    validation: [],
    accessibility: {}
  },

  divider: {
    type: 'divider',
    title: t('Divider'),
    description: t('Visual divider'),
    icon: 'mdi-minus',
    category: 'display',
    properties: {
      style: { type: 'string', title: 'Style', enum: ['solid', 'dashed', 'dotted'], default: 'solid' },
      thickness: { type: 'number', title: 'Thickness (px)', minimum: 1, default: 1 }
    },
    defaultProps: {
      style: 'solid',
      thickness: 1
    },
    validation: [],
    accessibility: {
      role: 'separator'
    }
  },

  // Special Components
  consent: {
    type: 'consent',
    title: t('Consent Checkbox'),
    description: t('Consent agreement checkbox'),
    icon: 'mdi-shield-check',
    category: 'special',
    properties: {
      text: { type: 'string', title: 'Consent Text', default: 'I agree to the terms and conditions' },
      required: { type: 'boolean', title: 'Required', default: true }
    },
    defaultProps: {
      text: 'I agree to the terms and conditions',
      required: true
    },
    validation: [
      { type: 'required', message: t('You must agree to continue') }
    ],
    accessibility: {
      role: 'checkbox'
    }
  }
}

// Get components by category
export const getComponentsByCategory = (category: string) => {
  return Object.values(componentRegistry).filter(component => component.category === category)
}

// Get all categories
export const getCategories = () => {
  const categories = new Set(Object.values(componentRegistry).map(c => c.category))
  return Array.from(categories)
}

// Predefined Group Templates
export const groupTemplates: GroupTemplate[] = [
  {
    id: 'contact-info',
    name: t('Contact Information'),
    description: t('Basic contact details'),
    category: 'contact',
    icon: 'mdi-account',
    previewText: 'Name, Email, Phone',
    tags: ['contact', 'basic'],
    isSystem: true,
    items: [
      {
        id: 'first_name',
        type: 'text',
        label: t('First Name'),
        required: true,
        props: { placeholder: 'Enter first name' }
      },
      {
        id: 'last_name',
        type: 'text',
        label: t('Last Name'),
        required: true,
        props: { placeholder: 'Enter last name' }
      },
      {
        id: 'email',
        type: 'email',
        label: t('Email Address'),
        required: true,
        props: { placeholder: 'Enter email address' }
      },
      {
        id: 'phone',
        type: 'phone',
        label: t('Phone Number'),
        props: { placeholder: 'Enter phone number' }
      }
    ]
  },
  {
    id: 'workplace-info',
    name: t('Workplace Information'),
    description: t('Employment and workplace details'),
    category: 'employment',
    icon: 'mdi-office-building',
    previewText: 'Company, Position, Address',
    tags: ['employment', 'workplace', 'repeater'],
    isSystem: true,
    items: [
      {
        id: 'company_name',
        type: 'text',
        label: t('Company Name'),
        required: true,
        props: { placeholder: 'Enter company name' }
      },
      {
        id: 'position',
        type: 'text',
        label: t('Position'),
        required: true,
        props: { placeholder: 'Enter your position' }
      },
      {
        id: 'work_address',
        type: 'address',
        label: t('Work Address'),
        props: {
          fields: ['street', 'city', 'state', 'zip'],
          defaultCountry: 'US'
        }
      },
      {
        id: 'start_date',
        type: 'date',
        label: t('Start Date'),
        props: { format: 'YYYY-MM-DD' }
      },
      {
        id: 'current_position',
        type: 'toggle',
        label: t('Current Position'),
        props: { trueLabel: 'Yes', falseLabel: 'No' }
      }
    ]
  },
  {
    id: 'education-info',
    name: t('Education Information'),
    isSystem: true,
    description: t('Educational background and qualifications'),
    category: 'education',
    icon: 'mdi-school',
    previewText: 'Institution, Degree, Field of Study',
    tags: ['education', 'academic', 'repeater'],
    items: [
      {
        id: 'institution_name',
        type: 'text',
        label: t('Institution Name'),
        required: true,
        props: { placeholder: 'Enter institution name' }
      },
      {
        id: 'degree_type',
        type: 'select',
        label: t('Degree Type'),
        required: true,
        props: {
          options: [
            { value: 'high_school', label: 'High School Diploma' },
            { value: 'associate', label: 'Associate Degree' },
            { value: 'bachelor', label: 'Bachelor\'s Degree' },
            { value: 'master', label: 'Master\'s Degree' },
            { value: 'doctorate', label: 'Doctorate' },
            { value: 'certificate', label: 'Certificate' },
            { value: 'other', label: 'Other' }
          ],
          placeholder: 'Select degree type'
        }
      },
      {
        id: 'field_of_study',
        type: 'text',
        label: t('Field of Study'),
        props: { placeholder: 'Enter field of study' }
      },
      {
        id: 'graduation_date',
        type: 'date',
        label: t('Graduation Date'),
        props: { format: 'YYYY-MM-DD' }
      },
      {
        id: 'gpa',
        type: 'number',
        label: t('GPA'),
        props: { min: 0, max: 4, step: 0.1, placeholder: 'Enter GPA' }
      }
    ]
  },
  {
    id: 'emergency-contact',
    name: t('Emergency Contact'),
    description: t('Emergency contact information'),
    category: 'contact',
    icon: 'mdi-account-alert',
    previewText: 'Name, Relationship, Phone',
    tags: ['contact', 'emergency'],
    isSystem: true,
    items: [
      {
        id: 'emergency_name',
        type: 'text',
        label: t('Contact Name'),
        required: true,
        props: { placeholder: 'Enter contact name' }
      },
      {
        id: 'relationship',
        type: 'select',
        label: t('Relationship'),
        required: true,
        props: {
          options: [
            { value: 'spouse', label: 'Spouse' },
            { value: 'parent', label: 'Parent' },
            { value: 'sibling', label: 'Sibling' },
            { value: 'child', label: 'Child' },
            { value: 'friend', label: 'Friend' },
            { value: 'other', label: 'Other' }
          ],
          placeholder: 'Select relationship'
        }
      },
      {
        id: 'emergency_phone',
        type: 'phone',
        label: t('Phone Number'),
        required: true,
        props: { placeholder: 'Enter phone number' }
      },
      {
        id: 'emergency_email',
        type: 'email',
        label: t('Email Address'),
        props: { placeholder: 'Enter email address' }
      }
    ]
  },
  {
    id: 'billing-address',
    name: t('Billing Address'),
    description: t('Billing and payment address'),
    category: 'address',
    icon: 'mdi-credit-card',
    previewText: 'Complete billing address',
    tags: ['address', 'billing'],
    isSystem: true,
    items: [
      {
        id: 'billing_address',
        type: 'address',
        label: t('Billing Address'),
        required: true,
        props: {
          fields: ['street', 'city', 'state', 'zip', 'country'],
          defaultCountry: 'US',
          validateAddress: true
        }
      },
      {
        id: 'same_as_shipping',
        type: 'toggle',
        label: t('Same as shipping address'),
        props: { trueLabel: 'Yes', falseLabel: 'No' }
      }
    ]
  },
  {
    id: 'medical-info',
    name: t('Medical Information'),
    description: t('Basic medical and health information'),
    category: 'medical',
    icon: 'mdi-medical-bag',
    previewText: 'Conditions, Medications, Allergies',
    tags: ['medical', 'health'],
    isSystem: true,
    items: [
      {
        id: 'medical_conditions',
        type: 'textarea',
        label: t('Medical Conditions'),
        props: { placeholder: 'List any medical conditions', rows: 3 }
      },
      {
        id: 'medications',
        type: 'textarea',
        label: t('Current Medications'),
        props: { placeholder: 'List current medications', rows: 3 }
      },
      {
        id: 'allergies',
        type: 'textarea',
        label: t('Known Allergies'),
        props: { placeholder: 'List any known allergies', rows: 2 }
      },
      {
        id: 'blood_type',
        type: 'select',
        label: t('Blood Type'),
        props: {
          options: [
            { value: 'a_positive', label: 'A+' },
            { value: 'a_negative', label: 'A-' },
            { value: 'b_positive', label: 'B+' },
            { value: 'b_negative', label: 'B-' },
            { value: 'ab_positive', label: 'AB+' },
            { value: 'ab_negative', label: 'AB-' },
            { value: 'o_positive', label: 'O+' },
            { value: 'o_negative', label: 'O-' },
            { value: 'unknown', label: 'Unknown' }
          ],
          placeholder: 'Select blood type'
        }
      }
    ]
  },
  {
    id: 'payment-info',
    name: t('Payment Information'),
    description: t('Credit card and payment details'),
    category: 'payment',
    icon: 'mdi-credit-card-outline',
    previewText: 'Card Number, Expiry, CVV',
    tags: ['payment', 'billing'],
    isSystem: true,
    items: [
      {
        id: 'card_number',
        type: 'text',
        label: t('Card Number'),
        required: true,
        props: { placeholder: '1234 5678 9012 3456', mask: '#### #### #### ####' }
      },
      {
        id: 'card_holder_name',
        type: 'text',
        label: t('Cardholder Name'),
        required: true,
        props: { placeholder: 'Name as on card' }
      },
      {
        id: 'expiry_date',
        type: 'text',
        label: t('Expiry Date'),
        required: true,
        props: { placeholder: 'MM/YY', mask: '##/##' }
      },
      {
        id: 'cvv',
        type: 'text',
        label: t('CVV'),
        required: true,
        props: { placeholder: '123', mask: '###' }
      }
    ]
  },
  {
    id: 'shipping-address',
    name: t('Shipping Address'),
    description: t('Delivery and shipping address'),
    category: 'address',
    icon: 'mdi-truck-delivery',
    previewText: 'Complete shipping address',
    tags: ['address', 'shipping'],
    isSystem: true,
    items: [
      {
        id: 'shipping_address',
        type: 'address',
        label: t('Shipping Address'),
        required: true,
        props: {
          fields: ['street', 'city', 'state', 'zip', 'country'],
          defaultCountry: 'US',
          validateAddress: true
        }
      },
      {
        id: 'delivery_instructions',
        type: 'textarea',
        label: t('Delivery Instructions'),
        props: { placeholder: 'Special delivery instructions...', rows: 2 }
      }
    ]
  },
  {
    id: 'identification',
    name: t('Identification'),
    description: t('Government ID and identification'),
    category: 'identity',
    icon: 'mdi-card-account-details',
    previewText: 'ID Type, Number, Expiry',
    tags: ['identity', 'verification'],
    isSystem: true,
    items: [
      {
        id: 'id_type',
        type: 'select',
        label: t('ID Type'),
        required: true,
        props: {
          options: [
            { value: 'drivers_license', label: 'Driver\'s License' },
            { value: 'passport', label: 'Passport' },
            { value: 'state_id', label: 'State ID' },
            { value: 'military_id', label: 'Military ID' },
            { value: 'other', label: 'Other' }
          ],
          placeholder: 'Select ID type'
        }
      },
      {
        id: 'id_number',
        type: 'text',
        label: t('ID Number'),
        required: true,
        props: { placeholder: 'Enter ID number' }
      },
      {
        id: 'id_expiry',
        type: 'date',
        label: t('Expiry Date'),
        props: { format: 'YYYY-MM-DD' }
      },
      {
        id: 'issuing_state',
        type: 'text',
        label: t('Issuing State/Country'),
        props: { placeholder: 'Enter issuing location' }
      }
    ]
  },
  {
    id: 'vehicle-info',
    name: t('Vehicle Information'),
    description: t('Vehicle registration and details'),
    category: 'vehicle',
    icon: 'mdi-car',
    previewText: 'Make, Model, Year, License',
    tags: ['vehicle', 'registration'],
    isSystem: true,
    items: [
      {
        id: 'vehicle_make',
        type: 'text',
        label: t('Make'),
        required: true,
        props: { placeholder: 'Toyota, Honda, Ford...' }
      },
      {
        id: 'vehicle_model',
        type: 'text',
        label: t('Model'),
        required: true,
        props: { placeholder: 'Camry, Civic, F-150...' }
      },
      {
        id: 'vehicle_year',
        type: 'number',
        label: t('Year'),
        required: true,
        props: { min: 1900, max: 2030, placeholder: '2020' }
      },
      {
        id: 'license_plate',
        type: 'text',
        label: t('License Plate'),
        props: { placeholder: 'ABC-1234' }
      },
      {
        id: 'vehicle_color',
        type: 'text',
        label: t('Color'),
        props: { placeholder: 'Black, White, Red...' }
      }
    ]
  },
  {
    id: 'insurance-info',
    name: t('Insurance Information'),
    description: t('Insurance policy and coverage details'),
    category: 'insurance',
    icon: 'mdi-shield-account',
    previewText: 'Provider, Policy Number, Coverage',
    tags: ['insurance', 'coverage'],
    isSystem: true,
    items: [
      {
        id: 'insurance_provider',
        type: 'text',
        label: t('Insurance Provider'),
        required: true,
        props: { placeholder: 'Enter insurance company name' }
      },
      {
        id: 'policy_number',
        type: 'text',
        label: t('Policy Number'),
        required: true,
        props: { placeholder: 'Enter policy number' }
      },
      {
        id: 'group_number',
        type: 'text',
        label: t('Group Number'),
        props: { placeholder: 'Enter group number' }
      },
      {
        id: 'effective_date',
        type: 'date',
        label: t('Effective Date'),
        props: { format: 'YYYY-MM-DD' }
      },
      {
        id: 'expiry_date',
        type: 'date',
        label: t('Expiry Date'),
        props: { format: 'YYYY-MM-DD' }
      }
    ]
  },
  {
    id: 'social-media',
    name: t('Social Media Profiles'),
    description: t('Social media accounts and profiles'),
    category: 'social',
    icon: 'mdi-share-variant',
    previewText: 'LinkedIn, Twitter, Facebook',
    tags: ['social', 'profiles'],
    isSystem: true,
    items: [
      {
        id: 'linkedin_url',
        type: 'text',
        label: t('LinkedIn Profile'),
        props: { placeholder: 'https://linkedin.com/in/...' }
      },
      {
        id: 'twitter_handle',
        type: 'text',
        label: t('Twitter Handle'),
        props: { placeholder: '@username' }
      },
      {
        id: 'facebook_url',
        type: 'text',
        label: t('Facebook Profile'),
        props: { placeholder: 'https://facebook.com/...' }
      },
      {
        id: 'instagram_handle',
        type: 'text',
        label: t('Instagram Handle'),
        props: { placeholder: '@username' }
      },
      {
        id: 'portfolio_website',
        type: 'text',
        label: t('Portfolio Website'),
        props: { placeholder: 'https://yourwebsite.com' }
      }
    ]
  },
  {
    id: 'skills-certifications',
    name: t('Skills & Certifications'),
    description: t('Professional skills and certifications'),
    category: 'professional',
    icon: 'mdi-certificate',
    previewText: 'Skills, Certifications, Languages',
    tags: ['skills', 'professional'],
    isSystem: true,
    items: [
      {
        id: 'technical_skills',
        type: 'textarea',
        label: t('Technical Skills'),
        props: { placeholder: 'List your technical skills...', rows: 3 }
      },
      {
        id: 'certifications',
        type: 'textarea',
        label: t('Certifications'),
        props: { placeholder: 'List your certifications...', rows: 3 }
      },
      {
        id: 'languages',
        type: 'textarea',
        label: t('Languages'),
        props: { placeholder: 'Languages and proficiency levels...', rows: 2 }
      },
      {
        id: 'years_experience',
        type: 'number',
        label: t('Years of Experience'),
        props: { min: 0, max: 50, placeholder: '5' }
      }
    ]
  },
  {
    id: 'dietary-preferences',
    name: t('Dietary Preferences'),
    description: t('Food allergies and dietary restrictions'),
    category: 'dietary',
    icon: 'mdi-food',
    previewText: 'Allergies, Restrictions, Preferences',
    tags: ['food', 'dietary', 'allergies'],
    isSystem: true,
    items: [
      {
        id: 'food_allergies',
        type: 'textarea',
        label: t('Food Allergies'),
        props: { placeholder: 'List any food allergies...', rows: 2 }
      },
      {
        id: 'dietary_restrictions',
        type: 'checkbox',
        label: t('Dietary Restrictions'),
        props: {
          options: [
            { value: 'vegetarian', label: 'Vegetarian' },
            { value: 'vegan', label: 'Vegan' },
            { value: 'gluten_free', label: 'Gluten-Free' },
            { value: 'kosher', label: 'Kosher' },
            { value: 'halal', label: 'Halal' },
            { value: 'dairy_free', label: 'Dairy-Free' }
          ],
          layout: 'vertical'
        }
      },
      {
        id: 'preferred_cuisine',
        type: 'text',
        label: t('Preferred Cuisine'),
        props: { placeholder: 'Italian, Asian, Mexican...' }
      }
    ]
  },
  {
    id: 'travel-info',
    name: t('Travel Information'),
    description: t('Travel preferences and requirements'),
    category: 'travel',
    icon: 'mdi-airplane',
    previewText: 'Passport, Preferences, Requirements',
    tags: ['travel', 'passport'],
    isSystem: true,
    items: [
      {
        id: 'passport_number',
        type: 'text',
        label: t('Passport Number'),
        props: { placeholder: 'Enter passport number' }
      },
      {
        id: 'passport_expiry',
        type: 'date',
        label: t('Passport Expiry'),
        props: { format: 'YYYY-MM-DD' }
      },
      {
        id: 'nationality',
        type: 'text',
        label: t('Nationality'),
        props: { placeholder: 'Enter nationality' }
      },
      {
        id: 'travel_preferences',
        type: 'checkbox',
        label: t('Travel Preferences'),
        props: {
          options: [
            { value: 'aisle_seat', label: 'Aisle Seat' },
            { value: 'window_seat', label: 'Window Seat' },
            { value: 'special_meal', label: 'Special Meal Required' },
            { value: 'wheelchair_assistance', label: 'Wheelchair Assistance' }
          ],
          layout: 'vertical'
        }
      }
    ]
  },
  {
    id: 'family-info',
    name: t('Family Information'),
    description: t('Spouse and family member details'),
    category: 'family',
    icon: 'mdi-account-group',
    previewText: 'Spouse, Children, Dependents',
    tags: ['family', 'dependents'],
    isSystem: true,
    items: [
      {
        id: 'marital_status',
        type: 'select',
        label: t('Marital Status'),
        props: {
          options: [
            { value: 'single', label: 'Single' },
            { value: 'married', label: 'Married' },
            { value: 'divorced', label: 'Divorced' },
            { value: 'widowed', label: 'Widowed' },
            { value: 'separated', label: 'Separated' }
          ],
          placeholder: 'Select marital status'
        }
      },
      {
        id: 'spouse_name',
        type: 'text',
        label: t('Spouse Name'),
        props: { placeholder: 'Enter spouse name' }
      },
      {
        id: 'number_of_children',
        type: 'number',
        label: t('Number of Children'),
        props: { min: 0, max: 20, placeholder: '0' }
      },
      {
        id: 'dependents',
        type: 'textarea',
        label: t('Dependents'),
        props: { placeholder: 'List any dependents...', rows: 2 }
      }
    ]
  },
  {
    id: 'event-feedback',
    name: t('Event Feedback'),
    description: t('Event evaluation and feedback form'),
    category: 'feedback',
    icon: 'mdi-star',
    previewText: 'Rating, Comments, Suggestions',
    tags: ['feedback', 'rating', 'event'],
    isSystem: true,
    items: [
      {
        id: 'overall_rating',
        type: 'select',
        label: t('Overall Rating'),
        required: true,
        props: {
          options: [
            { value: '5', label: '⭐⭐⭐⭐⭐ Excellent' },
            { value: '4', label: '⭐⭐⭐⭐ Good' },
            { value: '3', label: '⭐⭐⭐ Average' },
            { value: '2', label: '⭐⭐ Poor' },
            { value: '1', label: '⭐ Very Poor' }
          ],
          placeholder: 'Rate the event'
        }
      },
      {
        id: 'what_liked_most',
        type: 'textarea',
        label: t('What did you like most?'),
        props: { placeholder: 'Tell us what you enjoyed...', rows: 3 }
      },
      {
        id: 'suggestions',
        type: 'textarea',
        label: t('Suggestions for Improvement'),
        props: { placeholder: 'How can we improve?', rows: 3 }
      },
      {
        id: 'recommend_to_others',
        type: 'radio',
        label: t('Would you recommend this to others?'),
        props: {
          options: [
            { value: 'yes', label: 'Yes, definitely' },
            { value: 'maybe', label: 'Maybe' },
            { value: 'no', label: 'No' }
          ],
          layout: 'horizontal'
        }
      }
    ]
  }
]

// Get group templates by category
export const getGroupTemplatesByCategory = (category: string) => {
  return groupTemplates.filter(template => template.category === category)
}

// Get all group template categories
export const getGroupTemplateCategories = () => {
  const categories = new Set(groupTemplates.map(t => t.category))
  return Array.from(categories)
}

// Create a new group from template
export const createGroupFromTemplate = (template: GroupTemplate, instanceId?: string) => {
  const id = instanceId || `group_${template.id}_${Date.now()}`

  // Handle both old (items) and new (group) template structures
  const templateItems = template.group?.items || template.items || []

  return {
    id,
    title: template.name,
    description: template.description,
    items: templateItems.map(item => ({
      ...item,
      id: `${id}_${item.id}`
    })),
    isRepeater: template.group?.isRepeater || false,
    templateId: template.id,
    isTemplate: false
  }
}