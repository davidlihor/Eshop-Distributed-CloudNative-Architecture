import { Remove, Add, Delete } from '@mui/icons-material';
import { LoadingButton } from '@mui/lab';
import { TableContainer, Paper, Table, TableHead, TableRow, TableCell, TableBody, Box } from '@mui/material';
import {  useAppDispatch } from "../../app/store/configureStore";
import { deleteBasketItemAsync, updateBasketItemAsync } from "./basketSlice";
import { BasketItem } from '../../app/models/basket';

interface Props {
    items: BasketItem[];
    isBasket?: boolean;
}

export default function BasketTable({ items, isBasket = true }: Props) {
   // const { loading } = useAppSelector(state => state.basket);
    const dispatch = useAppDispatch();
   

    return (
        <TableContainer component={Paper}>
            <Table sx={{ minWidth: 650 }}>
                <TableHead>
                    <TableRow>
                        <TableCell>Product</TableCell>
                        <TableCell align="right">Price</TableCell>
                        <TableCell align="center">Quantity</TableCell>
                        <TableCell align="right">Subtotal</TableCell>
                        {isBasket &&
                            <TableCell align="right"></TableCell>}
                    </TableRow>
                </TableHead>
                <TableBody>
                    {items.map(item => (
                        <TableRow
                            key={item.productId}
                            sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                            style={{"backgroundColor": (item.isDeleteing ? "rgba(221, 221, 221, 1)" : "")}}
                        >
                            <TableCell component="th" scope="row">
                                <Box display='flex' alignItems='center'>
                                    <img src={item.pictureUrl} alt={item.title} style={{ height: 50, marginRight: 20 }} />
                                    <span>{item.title}</span>
                                </Box>
                            </TableCell>
                            <TableCell align="right">${item.price}</TableCell>
                            <TableCell align="center">
                                {isBasket &&
                                    <LoadingButton
                                        onClick={() => dispatch(updateBasketItemAsync({productId: item.productId, quantity: item.quantity - 1}))}
                                        color='error'
                                        disabled={item.isUpdating || item.quantity == 1}
                                    >
                                        <Remove />
                                    </LoadingButton>}
                                {item.quantity}
                                {isBasket &&
                                    <LoadingButton
                                        onClick={() => dispatch(updateBasketItemAsync({productId: item.productId, quantity: item.quantity + 1}))}
                                        color='secondary'
                                        disabled={item.isUpdating || item.quantity >= 50}
                                    >
                                        <Add />
                                    </LoadingButton>}
                            </TableCell>
                            <TableCell align="right">${Math.round((Number(item.price * item.quantity) * 100) / 100)}</TableCell>
                            {isBasket &&
                                <TableCell align="right">
                                    <LoadingButton
                                        loading={false}
                                        onClick={() => dispatch(deleteBasketItemAsync({productId: item.productId}))}
                                        color='error'
                                    >
                                        <Delete />
                                    </LoadingButton>
                                </TableCell>}
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </TableContainer>
    )
}