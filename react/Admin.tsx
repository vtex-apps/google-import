/* eslint-disable padding-line-between-statements */
/* eslint-disable no-console */
import React, { FC, useState, useEffect } from 'react'
import { useRuntime } from 'vtex.render-runtime'
import axios from 'axios'
import {
  Layout,
  PageHeader,
  Card,
  Button,
  ButtonPlain,
  Divider,
  Spinner,
} from 'vtex.styleguide'
import { injectIntl, FormattedMessage, WrappedComponentProps } from 'react-intl'

import styles from './styles.css'
import GoogleSignIn from '../public/metadata/google_signin.png'

const CHECK_URL = '/sheets-catalog-import/have-token'
const EMAIL_URL = '/sheets-catalog-import/owner-email'
const FETCH_URL = '/sheets-catalog-import/import-images'
const REVOKE_URL = '/sheets-catalog-import/revoke-token'
const AUTH_URL = '/sheets-catalog-import/auth'

let initialCheck = false

const Admin: FC<WrappedComponentProps> = ({ intl }) => {
  const [state, setState] = useState<any>({
    fetching: false,
    revoking: false,
    fetched: false,
    authorization: false,
    email: null,
    loading: true,
  })
  const { account } = useRuntime()

  const { fetching, revoking, fetched, authorization, email, loading } = state

  const fetch = () => {
    setState({
      ...state,
      fetching: true,
    })

    axios
      .get(FETCH_URL)
      .then((response: any) => {
        setState({
          ...state,
          fetching: false,
          fetched: response.data,
        })
        setTimeout(() => {
          setState({
            ...state,
            fetching: false,
            fetched: false,
          })
        }, 5000)
      })
      .catch(() => {
        setState({
          ...state,
          fetching: false,
          fetched: false,
        })
      })
  }

  const revoke = async () => {
    setState({
      ...state,
      revoking: true,
    })

    axios
      .get(REVOKE_URL)
      .then(() => {
        setState({
          ...state,
          revoking: false,
          authorization: false,
          email: null,
        })
      })
      .catch(() => {
        setState({
          ...state,
          revoking: false,
        })
      })

    return true
  }

  const auth = () => {
    revoke()
      .then(() => {
        window.top.location.href = AUTH_URL
      })
      .catch(() => {
        window.top.location.href = AUTH_URL
      })
  }

  useEffect(() => {
    if (initialCheck) return
    initialCheck = true
    let accountConnected = false
    axios
      .get(CHECK_URL)
      .then((response: any) => {
        accountConnected = response.data
        setState({
          ...state,
          loading: false,
          authorization: accountConnected,
        })
      })
      .catch(() => {
        setState({
          ...state,
          loading: false,
        })
      })
      .then(() => {
        if (!accountConnected) return
        axios.get(EMAIL_URL).then((response: any) => {
          setState({
            ...state,
            loading: false,
            authorization: accountConnected,
            email: response.data,
          })
        })
      })
  })

  return (
    <Layout
      pageHeader={
        <div className="flex justify-center">
          <div className="w-100 mw-reviews-header">
            <PageHeader
              title={intl.formatMessage({
                id: 'admin/sheets-catalog-import.title',
              })}
            />
          </div>
        </div>
      }
      fullWidth
    >
      <Card>
        {authorization && (
          <div className="flex">
            <div className="w-60">
              <h3 className="heading-3 mt4 mb4">
                <FormattedMessage id="admin/sheets-catalog-import.sku-images.title" />
              </h3>
              <p>
                <FormattedMessage
                  id="admin/sheets-catalog-import.connected.text"
                  values={{ lineBreak: <br /> }}
                />
              </p>
              <pre className={`${styles.code}`}>
                <FormattedMessage
                  id="admin/sheets-catalog-import.folder-structure"
                  values={{ lineBreak: <br />, account }}
                />
              </pre>
              <p>
                <FormattedMessage id="admin/sheets-catalog-import.instructions-line-01" />
              </p>

              <table className={`${styles.borderCollapse} ba collapse w-100`}>
                <thead>
                  <tr>
                    <th />
                    <th className="pa4">Description</th>
                  </tr>
                </thead>
                <tbody>
                  <tr>
                    <th className="flex justify-left bt items-center pa4">
                      IdType
                    </th>
                    <td className="bt bl pa4">
                      <FormattedMessage id="admin/sheets-catalog-import.instructions-description-IdType" />
                    </td>
                  </tr>
                  <tr className={`${styles.striped}`}>
                    <th className="flex justify-left bt pa4">Id</th>
                    <td className="bt bl pa4">
                      <FormattedMessage id="admin/sheets-catalog-import.instructions-description-Id" />
                    </td>
                  </tr>
                  <tr>
                    <th className="flex justify-left bt items-center pa4">
                      ImageName
                    </th>
                    <td className="bt bl pa4">
                      <FormattedMessage id="admin/sheets-catalog-import.instructions-description-ImageName" />
                    </td>
                  </tr>
                  <tr className={`${styles.striped}`}>
                    <th className="flex justify-left bt pa4">ImageLabel</th>
                    <td className="bt bl pa4">
                      <FormattedMessage id="admin/sheets-catalog-import.instructions-description-ImageLabel" />
                    </td>
                  </tr>
                  <tr className={`${styles.striped}`}>
                    <th className="flex justify-left bt pa4">Main?</th>
                    <td className="bt bl pa4">
                      <FormattedMessage id="admin/sheets-catalog-import.instructions-description-Main" />
                    </td>
                  </tr>
                  <tr className={`${styles.striped}`}>
                    <th className="flex justify-left bt pa4">Specification</th>
                    <td className="bt bl pa4">
                       <FormattedMessage id="admin/sheets-catalog-import.instructions-description-Spec" />
                    </td>
                  </tr>
                </tbody>
              </table>
              <p>
                <strong>
                  <FormattedMessage
                    id="admin/sheets-catalog-import.instructions-examples"
                    values={{ lineBreak: <br /> }}
                  />
                </strong>
              </p>
              <p>
                <FormattedMessage id="admin/sheets-catalog-import.instructions-line-02" />
              </p>
              <p>
                <FormattedMessage id="admin/sheets-catalog-import.instructions-line-03" />
              </p>
            </div>
            <div
              style={{ flexGrow: 1 }}
              className="flex items-stretch w-10 justify-center"
            >
              <Divider orientation="vertical" />
            </div>
            <div className="w-30">
              {email && (
                <p>
                  <FormattedMessage id="admin/sheets-catalog-import.connected-as" />{' '}
                  <strong>{`${email}`}</strong>
                </p>
              )}
              <div className="mt4 mb4">
                <Button
                  variation="danger-tertiary"
                  size="regular"
                  isLoading={revoking}
                  onClick={() => {
                    revoke()
                  }}
                  collapseLeft
                >
                  <FormattedMessage id="admin/sheets-catalog-import.disconnect.button" />
                </Button>
              </div>
              <Button
                variation="primary"
                collapseLeft
                isLoading={fetching}
                onClick={() => {
                  fetch()
                }}
              >
                <FormattedMessage id="admin/sheets-catalog-import.fetch.button" />
              </Button>
              {!fetching && fetched && (
                <p>
                  <strong>{`${fetched}`}</strong>
                </p>
              )}
            </div>
          </div>
        )}

        {!authorization && (
          <div>
            {loading && (
              <div className="pv6">
                <Spinner />
              </div>
            )}
            {!loading && (
              <div>
                <h2>
                  <FormattedMessage id="admin/sheets-catalog-import.setup.title" />
                </h2>
                <p>
                  <FormattedMessage id="admin/sheets-catalog-import.setup.description" />{' '}
                  <div className="mt4">
                    <ButtonPlain
                      variation="primary"
                      collapseLeft
                      onClick={() => {
                        auth()
                      }}
                    >
                      <img src={GoogleSignIn} alt="Sign in with Google" />
                    </ButtonPlain>
                  </div>
                </p>
              </div>
            )}
          </div>
        )}
      </Card>
    </Layout>
  )
}

export default injectIntl(Admin)
