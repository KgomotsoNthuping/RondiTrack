import { z } from 'zod'

export const registrationSchema = z
  .object({
    fullName: z
      .string()
      .trim()
      .min(1, 'Enter your full name.'),

    email: z
      .string()
      .trim()
      .email('Enter a valid email address.'),

    phoneNumber: z
      .string()
      .trim()
      .min(1, 'Enter your phone number.'),

    password: z
      .string()
      .min(
        12,
        'Password must be at least 12 characters.',
      ),

    confirmPassword: z
      .string()
      .min(1, 'Confirm your password.'),
  })
  .refine(
    values => values.password === values.confirmPassword,
    {
      message: 'Passwords do not match.',
      path: ['confirmPassword'],
    },
  )

export type RegistrationFormValues = z.infer<
  typeof registrationSchema
>
