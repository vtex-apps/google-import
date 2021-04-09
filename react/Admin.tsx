/* eslint-disable @typescript-eslint/no-explicit-any */
import React, { FC } from 'react'
import { useRuntime } from 'vtex.render-runtime'
import {
    Layout,
    PageHeader,
    Card,
    Button,
    ButtonPlain,
    Spinner,
    Divider,
} from 'vtex.styleguide'
import { injectIntl, FormattedMessage, WrappedComponentProps } from 'react-intl'
import { compose, graphql, useQuery, useMutation } from 'react-apollo'

//import styles from './styles.css'
import GoogleSignIn from '../public/metadata/google_signin.png'
import Q_OWNER_EMAIL from './queries/GetOwnerEmail.gql'
import Q_HAVE_TOKEN from './queries/HaveToken.gql'
import Q_SHEET_LINK from './queries/SheetLink.gql'
import M_REVOKE from './mutations/RevokeToken.gql'
// import M_AUTHORIZE from './mutations/GoogleAuthorize.gql'
import M_CREATE_SHEET from './mutations/CreateSheet.gql'
import M_PROCESS_SHEET from './mutations/ProcessSheet.gql'
import M_CLEAR_SHEET from './mutations/ClearSheet.gql'
import M_ADD_IMAGES from './mutations/AddImages.gql'

const AUTH_URL = '/sheets-catalog-import/auth'

const Admin: FC<WrappedComponentProps & any> = ({ intl, link, token }) => {
    const { account, pages } = useRuntime()

    const {
        loading: ownerLoading,
        called: ownerCalled,
        data: ownerData,
    } = useQuery(Q_OWNER_EMAIL, {
        variables: {
            accountName: account,
        },
    })

    const [revoke, { loading: revokeLoading }] = useMutation(M_REVOKE, {
        onCompleted: (ret: any) => {
            if (ret.revokeToken === true) {
                window.location.reload()
            }
        },
    })

    const [
        create,
        { loading: createLoading, data: createData, called: createCalled },
    ] = useMutation(M_CREATE_SHEET)

    const [
        sheetImport,
        { loading: sheetProcessing, data: sheetProcessed },
    ] = useMutation(M_PROCESS_SHEET)

    const auth = () => {
        revoke()
            .then(() => {
                window.top.location.href = AUTH_URL
            })
            .catch(() => {
                window.top.location.href = AUTH_URL
            })
    }

    const showLink = () => {
        return (
            (link.called && !link.loading && link.sheetLink) ||
            (createCalled && !createLoading && !!createData?.createSheet)
        )
    }

    const [
        clearSheet,
        { loading: sheetClearing, data: sheetCleared },
    ] = useMutation(M_CLEAR_SHEET)

    const [
        addImages,
        { loading: addingImages, data: imagesAdded },
    ] = useMutation(M_ADD_IMAGES)

    return (
        <Layout
            pageHeader={
                <div className="flex justify-center">
                    <div className="w-100 mw-reviews-header">
                        <PageHeader
                            title={intl.formatMessage({
                                id: 'admin/sheets-catalog-import.title',
                            })}
                        >
                            {token.called && !token.loading && token.haveToken === true && (
                                <div>
                                    {ownerCalled && !ownerLoading && ownerData && (
                                        <p>
                                            <FormattedMessage id="admin/sheets-catalog-import.connected-as" />{' '}
                                            <strong>{`${ownerData.getOwnerEmail}`}</strong>
                                        </p>
                                    )}
                                    <div className="mt4 mb4 tr">
                                        <Button
                                            variation="danger-tertiary"
                                            size="regular"
                                            isLoading={revokeLoading}
                                            onClick={() => {
                                                revoke({
                                                    variables: {
                                                        accountName: account,
                                                    },
                                                })
                                            }}
                                            collapseLeft
                                        >
                                            <FormattedMessage id="admin/sheets-catalog-import.disconnect.button" />
                                        </Button>
                                    </div>
                                </div>
                            )}
                        </PageHeader>
                    </div>
                </div>
            }
            fullWidth
        >
            {token.called && (
                <div>
                    {token.loading && (
                        <div className="pv6">
                            <Spinner />
                        </div>
                    )}
                    {!token.loading && token.haveToken !== true && (
                        <div>
                            <Card>
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
                            </Card>
                        </div>
                    )}
                </div>
            )}
            {token.called && !token.loading && token.haveToken === true && (
                        <div className="bg-base pa8">
                            <h2>
                                <FormattedMessage id="admin/sheets-catalog-import.catalog-product.title" />
                            </h2>

                            {!createData && link.called && !link.loading && !link.sheetLink && (
                                <Card>
                                    <div className="flex">
                                        <div className="w-70">
                                            <p>
                                                <FormattedMessage id="admin/sheets-catalog-import.create-sheet.description" />
                                            </p>
                                        </div>

                                        <div
                                            style={{ flexGrow: 1 }}
                                            className="flex items-stretch w-20 justify-center"
                                        >
                                            <Divider orientation="vertical" />
                                        </div>

                                        <div className="w-30 items-center flex">
                                            <Button
                                                variation="primary"
                                                collapseLeft
                                                block
                                                isLoading={createLoading}
                                                onClick={() => {
                                                    create()
                                                }}
                                            >
                                                <FormattedMessage id="admin/sheets-catalog-import.create-sheet.button" />
                                            </Button>
                                        </div>
                                    </div>
                                </Card>
                            )}
                            {showLink() && (
                                <Card>
                                    <div className="flex">
                                        <div className="w-100">
                                            <p>
                                                <FormattedMessage id="admin/sheets-catalog-import.sheet-link.description" />{' '}
                                                <a
                                                    href={createData?.createSheet || link.sheetLink}
                                                    target="_blank"
                                                    rel="noreferrer"
                                                >
                                                    {createData?.createSheet || link.sheetLink}
                                                </a>
                                            </p>
                                        </div>
                                    </div>
                                </Card>
                            )}
                            <br />
                            {showLink() && (
                                <div>
                                    <Card>
                                        <div className="flex">
                                            <div className="w-70">
                                                <p>
                                                    <FormattedMessage id="admin/sheets-catalog-import.sheet-import.description" />
                                                </p>
                                            </div>
                                            <div
                                                style={{ flexGrow: 1 }}
                                                className="flex items-stretch w-20 justify-center"
                                            >
                                                <Divider orientation="vertical" />
                                            </div>
                                            <div className="w-30 items-center flex">
                                                {!sheetProcessed?.processSheet && (
                                                    <Button
                                                        variation="primary"
                                                        collapseLeft
                                                        block
                                                        isLoading={sheetProcessing}
                                                        onClick={() => {
                                                            sheetImport()
                                                        }}
                                                    >
                                                        <FormattedMessage id="admin/sheets-catalog-import.sheet-import.button" />
                                                    </Button>
                                                )}
                                                {!sheetProcessing && sheetProcessed?.processSheet && (
                                                    <p>
                                                        <strong>{`${sheetProcessed.processSheet}`}</strong>
                                                    </p>
                                                )}
                                            </div>
                                        </div>
                                    </Card>
                                    <br />
                                </div>
                    )}
                    <br />
                    {showLink() && (
                        <div>
                            <Card>
                                <div className="flex">
                                    <div className="w-70">
                                        <p>
                                            <FormattedMessage id="admin/sheets-catalog-import.sheet-clear.description" />
                                        </p>
                                    </div>
                                    <div
                                        style={{ flexGrow: 1 }}
                                        className="flex items-stretch w-20 justify-center"
                                    >
                                        <Divider orientation="vertical" />
                                    </div>
                                    <div className="w-30 items-center flex">
                                        {!sheetCleared?.clearSheet && (
                                            <Button
                                                variation="primary"
                                                collapseLeft
                                                block
                                                isLoading={sheetClearing}
                                                onClick={() => {
                                                    clearSheet()
                                                }}
                                            >
                                                <FormattedMessage id="admin/sheets-catalog-import.sheet-clear.button" />
                                            </Button>
                                        )}
                                        {!sheetClearing && sheetCleared?.clearSheet && (
                                            <p>
                                                <strong>{`${sheetCleared.clearSheet}`}</strong>
                                            </p>
                                        )}
                                    </div>
                                </div>
                            </Card>
                            <br />
                        </div>
                    )}
                    <br />
                    {pages["admin.app.google-drive-import"] && showLink() && (
                        <div>
                            <Card>
                                <div className="flex">
                                    <div className="w-70">
                                        <p>
                                            <FormattedMessage id="admin/sheets-catalog-import.add-images.description" />
                                        </p>
                                    </div>
                                    <div
                                        style={{ flexGrow: 1 }}
                                        className="flex items-stretch w-20 justify-center"
                                    >
                                        <Divider orientation="vertical" />
                                    </div>
                                    <div className="w-30 items-center flex">
                                        {!imagesAdded?.addImages && (
                                            <Button
                                                variation="primary"
                                                collapseLeft
                                                block
                                                isLoading={addingImages}
                                                onClick={() => {
                                                    addImages()
                                                }}
                                            >
                                                <FormattedMessage id="admin/sheets-catalog-import.add-images.button" />
                                            </Button>
                                        )}
                                        {!addingImages && imagesAdded?.addImages && (
                                            <p>
                                                <strong>{`${imagesAdded.addImages}`}</strong>
                                            </p>
                                        )}
                                    </div>
                                </div>
                            </Card>
                            <br />
                        </div>
                    )}
                        </div>
            )}
        </Layout>
    )
}

const token = {
    name: 'token',
    options: () => ({
        ssr: false,
    }),
}

const link = {
    name: 'link',
    options: () => ({
        ssr: false,
    }),
}

export default injectIntl(
    compose(graphql(Q_HAVE_TOKEN, token), graphql(Q_SHEET_LINK, link))(Admin)
)
