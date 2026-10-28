import { LoginForm } from '@/components/LoginForm'

type LoginPageProps = {
  searchParams: Promise<{
    expired?: string
  }>
}

export default async function LoginPage({
  searchParams,
}: LoginPageProps) {
  const params = await searchParams

  return <LoginForm expired={params.expired === '1'} />
}
